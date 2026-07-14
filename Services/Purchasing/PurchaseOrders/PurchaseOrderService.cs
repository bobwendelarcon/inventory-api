using inventory_api.Data;
using inventory_api.DTOs.Purchasing.PurchaseOrders;
using inventory_api.Models.Purchasing.PurchaseOrders;

using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.PurchaseOrders
{
    public class PurchaseOrderService
    {
        private readonly AppDbContext _context;

        public PurchaseOrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GeneratePoNoAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"PO-{year}-";

            var lastPoNo = await _context.PurchaseOrderHeaders
                .Where(x => x.PoNo.StartsWith(prefix))
                .OrderByDescending(x => x.PoId)
                .Select(x => x.PoNo)
                .FirstOrDefaultAsync();

            var nextNo = 1;

            if (!string.IsNullOrWhiteSpace(lastPoNo))
            {
                var numberPart = lastPoNo.Replace(prefix, "");

                if (int.TryParse(numberPart, out var lastNumber))
                {
                    nextNo = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNo:0000}";
        }

        public async Task<int> CreateAsync(CreatePurchaseOrderDto dto, string userId)
        {
            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == dto.CanvassId);

            if (canvass == null)
                throw new Exception("Canvassing record not found.");

            //if (canvass.Status != "COMPLETED")
            //    throw new Exception("Only completed canvassing can create Purchase Order.");

            if (canvass.Status != "OPEN" && canvass.Status != "COMPLETED")
                throw new Exception("Only active canvassing can create Purchase Order.");

            if (dto.SupplierId <= 0)
                throw new Exception("Supplier is required.");

            if (dto.Lines == null || !dto.Lines.Any())
                throw new Exception("Purchase Order must have at least one material.");

            var poNo = await GeneratePoNoAsync();

            var subtotal = dto.Lines.Sum(x => x.PoQty * x.PoUnitPrice);
            var totalAmount = subtotal + dto.OtherCharges;

            var header = new PurchaseOrderHeader
            {
                PoNo = poNo,
                CanvassId = dto.CanvassId,
                SupplierId = dto.SupplierId,
                PoDate = dto.PoDate,
                DeliveryDate = dto.DeliveryDate,
                PaymentTerms = dto.PaymentTerms,
                Remarks = dto.Remarks,
                Status = "DRAFT",
                SupplierAddress = dto.SupplierAddress,
                RequestedBy = dto.RequestedBy,
                Subtotal = subtotal,
                OtherCharges = dto.OtherCharges,
                TotalAmount = totalAmount,
                PrintedPoNo = dto.PrintedPoNo,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            var requestedCanvassLineIds = dto.Lines
    .Select(x => x.CanvassLineId)
    .Distinct()
    .ToList();

            var duplicateLines = await _context.PurchaseOrderLines
                .Where(pol => requestedCanvassLineIds.Contains(pol.CanvassLineId))
                .Where(pol => pol.Header != null && pol.Header.Status != "CANCELLED")
                .Select(pol => pol.CanvassLineId)
                .Distinct()
                .ToListAsync();

            if (duplicateLines.Any())
                throw new Exception("One or more canvass lines already belong to an active Purchase Order.");


            foreach (var line in dto.Lines)
            {
                var validQuote = await _context.PurchasingCanvassQuotes
                    .AnyAsync(q =>
                        q.QuoteId == line.QuoteId &&
                        q.CanvassLineId == line.CanvassLineId &&
                        q.SupplierId == dto.SupplierId &&
                        q.IsRecommended);

                if (!validQuote)
                    throw new Exception("Selected material does not match the selected recommended supplier.");
            }

            foreach (var line in dto.Lines)
            {
                if (line.PoQty <= 0)
                    throw new Exception("PO quantity must be greater than zero.");

                if (line.PoUnitPrice <= 0)
                    throw new Exception("PO unit price must be greater than zero.");

                var lineTotal = line.PoQty * line.PoUnitPrice;

                header.Lines.Add(new PurchaseOrderLine
                {
                    CanvassLineId = line.CanvassLineId,
                    QuoteId = line.QuoteId,
                    MaterialId = line.MaterialId,

                    PoQty = line.PoQty,
                    Uom = line.Uom,

                    QuotationUnitPrice = line.QuotationUnitPrice,
                    PoUnitPrice = line.PoUnitPrice,
                    LineTotal = lineTotal,

                    Remarks = line.Remarks,

                    ReceivedQty = 0,
                    BalanceQty = line.PoQty,

                    Status = "OPEN",
                    CreatedAt = DateTime.Now
                });
            }

            _context.PurchaseOrderHeaders.Add(header);
            await _context.SaveChangesAsync();
            await UpdateMprfPoStatusAsync(dto.CanvassId);

            return header.PoId;
        }


        public async Task<object?> GetCreateOptionsAsync(int canvassId)
        {
            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == canvassId);

            if (canvass == null)
                return null;

            var activeCanvassLineIds = await _context.PurchaseOrderLines
                .Where(pol => pol.Header != null && pol.Header.Status != "CANCELLED")
                .Select(pol => pol.CanvassLineId)
                .Distinct()
                .ToListAsync();

            var remaining = await (
                from cl in _context.PurchasingCanvassLines
                join q in _context.PurchasingCanvassQuotes
                    on cl.CanvassLineId equals q.CanvassLineId
                join s in _context.Suppliers
                    on q.SupplierId equals s.SupplierId
                join m in _context.Materials
                    on cl.MaterialId equals m.material_id
                where cl.CanvassId == canvassId
                      && q.IsRecommended
                      && !activeCanvassLineIds.Contains(cl.CanvassLineId)
                select new
                {
                    cl.CanvassLineId,
                    q.QuoteId,
                    q.SupplierId,
                    s.SupplierName,
                    SupplierAddress = s.Address,
                    q.PaymentTerms,
                    cl.MaterialId,
                    m.material_code,
                    m.material_name,
                    Qty = cl.PurchasingQty,
                    Uom = cl.Uom ?? m.uom,
                    UnitPrice = q.UnitPrice
                }
            ).ToListAsync();

            var existingPoSuppliers = await (
    from po in _context.PurchaseOrderHeaders
    where po.CanvassId == canvassId
          && po.Status != "CANCELLED"
    select new
    {
        po.PoId,
        po.PoNo,
        po.SupplierId,
        po.Status,
        po.TotalAmount
    }
).ToListAsync();

            return new
            {
                suppliers = remaining
                    .GroupBy(x => new { x.SupplierId, x.SupplierName, x.SupplierAddress, x.PaymentTerms })
                    .Select(g => new
                    {
                        g.Key.SupplierId,
                        g.Key.SupplierName,
                        g.Key.SupplierAddress,
                        g.Key.PaymentTerms,
                        LineCount = g.Count(),
                        TotalAmount = g.Sum(x => x.Qty * x.UnitPrice)
                    })
                    .ToList(),

                lines = remaining,
                existing_po_suppliers = existingPoSuppliers
            };




        }
        public async Task<List<PurchaseOrderListDto>> GetAllAsync()
        {
            var data = await _context.PurchaseOrderHeaders
                .OrderByDescending(x => x.PoId)
                .Select(x => new PurchaseOrderListDto
                {
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    PoDate = x.PoDate,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    TotalAmount = x.TotalAmount,
                    Status = x.Status,

                    CreatedBy = x.CreatedBy,

                    CreatedByName = _context.Users
    .Where(u => u.user_id == x.CreatedBy)
    .Select(u => u.full_name)
    .FirstOrDefault() ?? x.CreatedBy,

                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return data;
        }

        public async Task<PurchaseOrderDetailsDto?> GetByIdAsync(int poId)
        {
            var data = await _context.PurchaseOrderHeaders
                .Where(x => x.PoId == poId)
                .Select(x => new PurchaseOrderDetailsDto
                {
                    PoId = x.PoId,
                    PoNo = x.PoNo,
                    CanvassId = x.CanvassId,
                    SupplierId = x.SupplierId,

                    SupplierName = _context.Suppliers
                        .Where(s => s.SupplierId == x.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "",

                    SupplierAddress = x.SupplierAddress,
                    RequestedBy = x.RequestedBy,

                    Subtotal = x.Subtotal,
                    OtherCharges = x.OtherCharges,
                    TotalAmount = x.TotalAmount,

                    CheckedBy = x.CheckedBy,
                    CheckedAt = x.CheckedAt,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedAt = x.ApprovedAt,

                    PoDate = x.PoDate,
                    DeliveryDate = x.DeliveryDate,
                    PaymentTerms = x.PaymentTerms,
                    Remarks = x.Remarks,
                    Status = x.Status,
                    PrintedPoNo = x.PrintedPoNo,

                    Lines = x.Lines.Select(l => new PurchaseOrderLineDto
                    {
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
                        Uom = l.Uom,
                        QuotationUnitPrice = l.QuotationUnitPrice,
                        PoUnitPrice = l.PoUnitPrice,
                        LineTotal = l.LineTotal,

                        ReceivedQty = l.ReceivedQty,
                        BalanceQty = l.BalanceQty,

                        Status = l.Status,
                        Remarks = l.Remarks
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return data;
        }

        public async Task SubmitForApprovalAsync(int poId)
        {
            var po = await _context.PurchaseOrderHeaders
                .FirstOrDefaultAsync(x => x.PoId == poId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Status != "DRAFT")
                throw new Exception("Only draft PO can be submitted for approval.");

            po.Status = "FOR_APPROVAL";
            po.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ApproveAsync(int poId, string userId)
        {
            var po = await _context.PurchaseOrderHeaders
                .FirstOrDefaultAsync(x => x.PoId == poId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Status != "FOR_APPROVAL")
                throw new Exception("Only PO for approval can be approved.");

            po.Status = "APPROVED";
            po.ApprovedBy = userId;
            po.ApprovedAt = DateTime.Now;
            po.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task CancelAsync(int poId)
        {
            var po = await _context.PurchaseOrderHeaders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.PoId == poId);

            if (po == null)
                throw new Exception("Purchase Order not found.");

            if (po.Lines.Any(x => x.ReceivedQty > 0))
                throw new Exception("Cannot cancel PO because it already has received quantity.");

            po.Status = "CANCELLED";
            po.UpdatedAt = DateTime.Now;

            foreach (var line in po.Lines)
            {
                line.Status = "CANCELLED";
                line.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await UpdateMprfPoStatusAsync(po.CanvassId);
        }

        private async Task UpdateMprfPoStatusAsync(int canvassId)
        {
            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == canvassId);

            if (canvass == null) return;

            var mprf = await _context.PurchasingMprfHeaders
                .FirstOrDefaultAsync(x => x.mprf_id == canvass.MprfId);

            if (mprf == null) return;

            var totalRecommendedSuppliers = await (
                from cl in _context.PurchasingCanvassLines
                join q in _context.PurchasingCanvassQuotes
                    on cl.CanvassLineId equals q.CanvassLineId
                where cl.CanvassId == canvassId
                      && q.IsRecommended
                select q.SupplierId
            ).Distinct().CountAsync();

            var totalActivePoSuppliers = await _context.PurchaseOrderHeaders
                .Where(po => po.CanvassId == canvassId
                             && po.Status != "CANCELLED")
                .Select(po => po.SupplierId)
                .Distinct()
                .CountAsync();

            if (totalRecommendedSuppliers > 0 &&
                totalActivePoSuppliers >= totalRecommendedSuppliers)
            {
                mprf.status = "PO_CREATED";
            }
            else
            {
                mprf.status = "CANVASSING";
            }

            mprf.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}