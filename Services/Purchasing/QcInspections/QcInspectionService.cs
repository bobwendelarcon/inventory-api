using inventory_api.Data;
using inventory_api.DTOs.Purchasing.QcInspections;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.QcInspections
{
    public class QcInspectionService
    {
        private readonly AppDbContext _context;

        public QcInspectionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<QcInspectionListDto>> GetAllAsync()
        {
            return await _context.QcInspectionHeaders
                .OrderByDescending(x => x.QcId)
                .Select(x => new QcInspectionListDto
                {
                    QcId = x.QcId,
                    QcNo = x.QcNo,
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoNo = x.PoNo,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    InspectionDate = x.InspectionDate,
                    InspectorId = x.InspectorId,
                    Status = x.Status,
                    Decision = x.Decision,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<QcInspectionDetailsDto?> GetByIdAsync(int qcId)
        {
            return await _context.QcInspectionHeaders
                .Where(x => x.QcId == qcId)
                .Select(x => new QcInspectionDetailsDto
                {
                    QcId = x.QcId,
                    QcNo = x.QcNo,
                    RrId = x.RrId,
                    RrNo = x.RrNo,
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    SupplierId = x.SupplierId,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    InspectionDate = x.InspectionDate,
                    InspectorId = x.InspectorId,
                    Status = x.Status,
                    Decision = x.Decision,
                    Remarks = x.Remarks,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CommittedBy = x.CommittedBy,
                    CommittedAt = x.CommittedAt,

                    Lines = x.Lines.Select(l => new QcInspectionLineDetailsDto
                    {
                        QcLineId = l.QcLineId,
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

                        ReceivedQty = l.ReceivedQty,
                        AcceptedQty = l.AcceptedQty,
                        RejectedQty = l.RejectedQty,
                        RejectionReason = l.RejectionReason,
                        Remarks = l.Remarks,
                        Status = l.Status
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task SaveInspectionAsync(int qcId, SaveQcInspectionDto dto, string userId)
        {
            var qc = await _context.QcInspectionHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.QcId == qcId);

            if (qc == null)
                throw new Exception("QC inspection not found.");

            if (qc.Status != "FOR_INSPECTION")
                throw new Exception("Only QC for inspection can be updated.");

            if (dto.Lines == null || !dto.Lines.Any())
                throw new Exception("QC inspection must have at least one line.");

            foreach (var lineDto in dto.Lines)
            {
                var line = qc.Lines.FirstOrDefault(x => x.QcLineId == lineDto.QcLineId);

                if (line == null)
                    throw new Exception("Invalid QC line.");

                if (lineDto.AcceptedQty < 0 || lineDto.RejectedQty < 0)
                    throw new Exception("Accepted and rejected quantity cannot be negative.");

                if (lineDto.AcceptedQty + lineDto.RejectedQty != line.ReceivedQty)
                    throw new Exception("Accepted quantity plus rejected quantity must equal received quantity.");

                line.AcceptedQty = lineDto.AcceptedQty;
                line.RejectedQty = lineDto.RejectedQty;
                line.RejectionReason = lineDto.RejectionReason;
                line.Remarks = lineDto.Remarks;

                if (lineDto.AcceptedQty == line.ReceivedQty)
                    line.Status = "ACCEPTED";
                else if (lineDto.RejectedQty == line.ReceivedQty)
                    line.Status = "REJECTED";
                else
                    line.Status = "PARTIALLY_ACCEPTED";

                line.UpdatedAt = DateTime.Now;
            }

            var totalAccepted = qc.Lines.Sum(x => x.AcceptedQty);
            var totalRejected = qc.Lines.Sum(x => x.RejectedQty);
            var totalReceived = qc.Lines.Sum(x => x.ReceivedQty);

            if (totalAccepted == totalReceived)
            {
                qc.Status = "ACCEPTED";
                qc.Decision = "ACCEPTED";
            }
            else if (totalRejected == totalReceived)
            {
                qc.Status = "REJECTED";
                qc.Decision = "REJECTED";
            }
            else
            {
                qc.Status = "PARTIALLY_ACCEPTED";
                qc.Decision = "PARTIALLY_ACCEPTED";
            }

            qc.InspectionDate = dto.InspectionDate ?? DateTime.Now;
            qc.InspectorId = !string.IsNullOrWhiteSpace(dto.InspectorId)
                ? dto.InspectorId
                : userId;

            qc.Remarks = dto.Remarks;
            qc.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
        public async Task CommitAsync(int qcId, string userId)
        {
            var qc = await _context.QcInspectionHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.QcId == qcId);

            if (qc == null)
                throw new Exception("QC inspection not found.");

            if (qc.Status != "ACCEPTED" && qc.Status != "PARTIALLY_ACCEPTED")
                throw new Exception("Only accepted or partially accepted QC can be committed.");

            var po = await _context.PurchaseOrderHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.PoId == qc.PoId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            foreach (var qcLine in qc.Lines)
            {
                if (qcLine.AcceptedQty <= 0)
                    continue;

                var poLine = po.Lines.FirstOrDefault(x => x.PoLineId == qcLine.PoLineId);

                if (poLine == null)
                    throw new Exception("PO line not found.");

                // IMPORTANT:
                // PO received_qty should now represent ACCEPTED quantity, not physical RR qty.
                poLine.ReceivedQty += qcLine.AcceptedQty;
                poLine.BalanceQty = poLine.PoQty - poLine.ReceivedQty;

                if (poLine.BalanceQty <= 0)
                {
                    poLine.BalanceQty = 0;
                    poLine.Status = "CLOSED";
                }
                else
                {
                    poLine.Status = "PARTIAL";
                }

                poLine.UpdatedAt = DateTime.Now;

                // Material Inventory update will be added here depending on your existing table fields.
                // For now, this is the correct place to insert inventory transaction.
            }

            var allClosed = po.Lines.All(x => x.BalanceQty <= 0);
            var anyAccepted = po.Lines.Any(x => x.ReceivedQty > 0);

            po.Status = allClosed
                ? "FULLY_RECEIVED"
                : anyAccepted
                    ? "PARTIALLY_RECEIVED"
                    : "APPROVED";

            po.UpdatedAt = DateTime.Now;

            qc.Status = "COMMITTED";
            qc.CommittedBy = userId;
            qc.CommittedAt = DateTime.Now;
            qc.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}