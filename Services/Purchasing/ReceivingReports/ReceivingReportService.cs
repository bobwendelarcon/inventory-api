using inventory_api.Data;
using inventory_api.DTOs.Purchasing.ReceivingReports;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.ReceivingReports;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.ReceivingReports
{
    public class ReceivingReportService
    {
        private readonly AppDbContext _context;

        public ReceivingReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateRrNoAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"RR-{year}-";

            var lastNo = await _context.ReceivingReportHeaders
                .Where(x => x.RrNo.StartsWith(prefix))
                .OrderByDescending(x => x.RrId)
                .Select(x => x.RrNo)
                .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart = lastNo.Replace(prefix, "");

                if (int.TryParse(numberPart, out var lastNumber))
                    nextNo = lastNumber + 1;
            }

            return $"{prefix}{nextNo:0000}";
        }

        public async Task<object?> GetCreateOptionsAsync(int poId)
        {
            var po = await _context.PurchaseOrderHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.PoId == poId);

            if (po == null)
                return null;

            if (po.Status != "APPROVED" && po.Status != "PARTIALLY_RECEIVED")
                throw new Exception("Only approved or partially received PO can create RR.");

            var supplierName = await _context.Suppliers
                .Where(x => x.SupplierId == po.SupplierId)
                .Select(x => x.SupplierName)
                .FirstOrDefaultAsync();

            return new
            {
                po.PoId,
                po.PoNo,
                po.SupplierId,
                SupplierName = supplierName ?? "",
                po.DeliveryDate,
                po.Status,
                Lines = po.Lines
                    .Where(x => x.BalanceQty > 0 && x.Status != "CLOSED")
                    .Select(x => new
                    {
                        x.PoLineId,
                        x.MaterialId,
                        MaterialCode = _context.Materials
                            .Where(m => m.material_id == x.MaterialId)
                            .Select(m => m.material_code)
                            .FirstOrDefault() ?? "",
                        MaterialName = _context.Materials
                            .Where(m => m.material_id == x.MaterialId)
                            .Select(m => m.material_name)
                            .FirstOrDefault() ?? "",
                        x.PoQty,
                        PreviouslyReceivedQty = x.ReceivedQty,
                        x.BalanceQty,
                        x.Uom
                    })
                    .ToList()
            };
        }

        public async Task<int> CreateAsync(CreateReceivingReportDto dto, string userId)
        {
            var po = await _context.PurchaseOrderHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.PoId == dto.PoId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Status != "APPROVED" && po.Status != "PARTIALLY_RECEIVED")
                throw new Exception("Only approved or partially received PO can create RR.");

            if (dto.Lines == null || !dto.Lines.Any())
                throw new Exception("RR must have at least one line.");

            var validLines = dto.Lines.Where(x => x.ReceiveQty > 0).ToList();

            if (!validLines.Any())
                throw new Exception("Receive quantity is required.");

            var rrNo = await GenerateRrNoAsync();

            var rr = new ReceivingReportHeader
            {
                RrNo = rrNo,
                PoId = po.PoId,
                PoNo = po.PoNo,
                SupplierId = po.SupplierId,
                SiDrNo = dto.SiDrNo,
                DeliveryDate = dto.DeliveryDate,
                Remarks = dto.Remarks,
                Status = "DRAFT",
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            foreach (var lineDto in validLines)
            {
                var poLine = po.Lines.FirstOrDefault(x => x.PoLineId == lineDto.PoLineId);

                if (poLine == null)
                    throw new Exception("Invalid PO line selected.");

                if (poLine.BalanceQty <= 0)
                    throw new Exception("Selected PO line has no remaining balance.");

                if (lineDto.ReceiveQty > poLine.BalanceQty)
                    throw new Exception("Receive quantity cannot exceed balance quantity.");

                rr.Lines.Add(new ReceivingReportLine
                {
                    PoLineId = poLine.PoLineId,
                    MaterialId = poLine.MaterialId,
                    PoQty = poLine.PoQty,
                    PreviouslyReceivedQty = poLine.ReceivedQty,
                    BalanceQty = poLine.BalanceQty,
                    ReceiveQty = lineDto.ReceiveQty,
                    AcceptedQty = 0,
                    RejectedQty = 0,
                    Uom = poLine.Uom,
                    Remarks = lineDto.Remarks,
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                });

                //poLine.ReceivedQty += lineDto.ReceiveQty;
                //poLine.BalanceQty = poLine.PoQty - poLine.ReceivedQty;
                //poLine.Status = poLine.BalanceQty <= 0 ? "CLOSED" : "PARTIAL";
                //poLine.UpdatedAt = DateTime.Now;
            }

            //var allClosed = po.Lines.All(x => x.BalanceQty <= 0);
            //var anyReceived = po.Lines.Any(x => x.ReceivedQty > 0);

            //po.Status = allClosed ? "FULLY_RECEIVED" : anyReceived ? "PARTIALLY_RECEIVED" : po.Status;
            //po.UpdatedAt = DateTime.Now;

            _context.ReceivingReportHeaders.Add(rr);

            await _context.SaveChangesAsync();

            return rr.RrId;
        }

        public async Task SubmitForQcAsync(int rrId)
        {
            var rr = await _context.ReceivingReportHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.RrId == rrId);

            if (rr == null)
                throw new Exception("RR not found.");

            if (rr.Status != "DRAFT")
                throw new Exception("Only draft RR can be submitted for QC.");

            var existingQc = await _context.QcInspectionHeaders
                .AnyAsync(x => x.RrId == rr.RrId);

            if (existingQc)
                throw new Exception("QC inspection already exists for this RR.");

            var qcNo = await GenerateQcNoAsync();

            var qc = new QcInspectionHeader
            {
                QcNo = qcNo,
                RrId = rr.RrId,
                RrNo = rr.RrNo,
                PoId = rr.PoId,
                PoNo = rr.PoNo,
                SupplierId = rr.SupplierId,
                Status = "FOR_INSPECTION",
                CreatedBy = rr.CreatedBy,
                CreatedAt = DateTime.Now
            };

            foreach (var rrLine in rr.Lines)
            {
                qc.Lines.Add(new QcInspectionLine
                {
                    RrLineId = rrLine.RrLineId,
                    PoLineId = rrLine.PoLineId,
                    MaterialId = rrLine.MaterialId,
                    ReceivedQty = rrLine.ReceiveQty,
                    AcceptedQty = 0,
                    RejectedQty = 0,
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                });
            }

            rr.Status = "FOR_QC";
            rr.UpdatedAt = DateTime.Now;

            _context.QcInspectionHeaders.Add(qc);

            await _context.SaveChangesAsync();
        }

        public async Task AcceptAsync(int rrId, string userId)
        {
            var rr = await _context.ReceivingReportHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.RrId == rrId);

            if (rr == null)
                throw new Exception("RR not found.");

            if (rr.Status != "FOR_QC")
                throw new Exception("Only RR for QC can be accepted.");

            rr.Status = "ACCEPTED";
            rr.QcBy = userId;
            rr.QcAt = DateTime.Now;
            rr.UpdatedAt = DateTime.Now;

            foreach (var line in rr.Lines)
            {
                line.AcceptedQty = line.ReceiveQty;
                line.RejectedQty = 0;
                line.Status = "ACCEPTED";
                line.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RejectAsync(int rrId, string userId)
        {
            var rr = await _context.ReceivingReportHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.RrId == rrId);

            if (rr == null)
                throw new Exception("RR not found.");

            if (rr.Status != "FOR_QC")
                throw new Exception("Only RR for QC can be rejected.");

            rr.Status = "REJECTED";
            rr.QcBy = userId;
            rr.QcAt = DateTime.Now;
            rr.UpdatedAt = DateTime.Now;

            foreach (var line in rr.Lines)
            {
                line.AcceptedQty = 0;
                line.RejectedQty = line.ReceiveQty;
                line.Status = "REJECTED";
                line.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CommitAsync(int rrId, string userId)
        {
            var rr = await _context.ReceivingReportHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.RrId == rrId);

            if (rr == null)
                throw new Exception("RR not found.");

            if (rr.Status != "ACCEPTED")
                throw new Exception("Only accepted RR can be committed.");

            // Inventory update will be added here next.
            // Do not update inventory during RR creation.

            rr.Status = "COMMITTED";
            rr.CommittedBy = userId;
            rr.CommittedAt = DateTime.Now;
            rr.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<List<ReceivingReportListDto>> GetAllAsync()
        {
            return await _context.ReceivingReportHeaders
                .OrderByDescending(x => x.RrId)
                .Select(x => new ReceivingReportListDto
                {
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoNo = x.PoNo,
                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",
                    SiDrNo = x.SiDrNo,
                    DeliveryDate = x.DeliveryDate,
                    Status = x.Status,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ReceivingReportDetailsDto?> GetByIdAsync(int rrId)
        {
            return await _context.ReceivingReportHeaders
                .Where(x => x.RrId == rrId)
                .Select(x => new ReceivingReportDetailsDto
                {
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    SupplierId = x.SupplierId,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    SiDrNo = x.SiDrNo,
                    DeliveryDate = x.DeliveryDate,
                    Remarks = x.Remarks,
                    Status = x.Status,

                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,

                    QcBy = x.QcBy,
                    QcAt = x.QcAt,
                    CommittedBy = x.CommittedBy,
                    CommittedAt = x.CommittedAt,

                    Lines = x.Lines.Select(l => new ReceivingReportLineDetailsDto
                    {
                        RrLineId = l.RrLineId,
                        PoLineId = l.PoLineId,
                        MaterialId = l.MaterialId,

                        MaterialCode = _context.Materials
                            .Where(m => m.material_id == l.MaterialId)
                            .Select(m => m.material_code)
                            .FirstOrDefault() ?? "",

                        MaterialName = _context.Materials
                            .Where(m => m.material_id == l.MaterialId)
                            .Select(m => m.material_name)
                            .FirstOrDefault() ?? "",

                        PoQty = l.PoQty,
                        PreviouslyReceivedQty = l.PreviouslyReceivedQty,
                        BalanceQty = l.BalanceQty,
                        ReceiveQty = l.ReceiveQty,
                        AcceptedQty = l.AcceptedQty,
                        RejectedQty = l.RejectedQty,
                        Uom = l.Uom,
                        Remarks = l.Remarks,
                        Status = l.Status
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        private async Task<string> GenerateQcNoAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"QC-{year}-";

            var lastNo = await _context.QcInspectionHeaders
                .Where(x => x.QcNo.StartsWith(prefix))
                .OrderByDescending(x => x.QcId)
                .Select(x => x.QcNo)
                .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastNo))
            {
                var numberPart = lastNo.Replace(prefix, "");

                if (int.TryParse(numberPart, out var lastNumber))
                    nextNo = lastNumber + 1;
            }

            return $"{prefix}{nextNo:0000}";
        }
    }
}