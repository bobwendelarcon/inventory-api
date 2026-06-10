using inventory_api.Data;
using inventory_api.DTOs.Purchasing.Canvassing;
using inventory_api.Models.Purchasing.Canvassing;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.Canvassing
{
    public class CanvassingService
    {
        private readonly AppDbContext _context;

        public CanvassingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetPagedAsync(
     string? search,
     string? status,
     int page,
     int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query =
                from c in _context.PurchasingCanvassHeaders
                join h in _context.PurchasingMprfHeaders
                    on c.MprfId equals h.mprf_id
                select new
                {
                    c.CanvassId,
                    c.CanvassNo,
                    c.CanvassDate,
                    c.Status,
                    MprfNo = h.mprf_no,
                    Department = h.category
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.CanvassNo.Contains(search) ||
                    x.MprfNo.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(x => x.Status == status);
            }

            var totalRecords = await query.CountAsync();

            var rows = await query
                .OrderByDescending(x => x.CanvassId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = new List<object>();

            foreach (var row in rows)
            {
                var totalRecommendedSuppliers = await (
                    from cl in _context.PurchasingCanvassLines
                    join q in _context.PurchasingCanvassQuotes
                        on cl.CanvassLineId equals q.CanvassLineId
                    where cl.CanvassId == row.CanvassId
                          && q.IsRecommended
                    select q.SupplierId
                ).Distinct().CountAsync();

                var totalCreatedPoSuppliers = await _context.PurchaseOrderHeaders
                    .Where(po => po.CanvassId == row.CanvassId
                                 && po.Status != "CANCELLED")
                    .Select(po => po.SupplierId)
                    .Distinct()
                    .CountAsync();

                data.Add(new
                {
                    row.CanvassId,
                    row.CanvassNo,
                    row.CanvassDate,
                    row.Status,
                    row.MprfNo,
                    row.Department,
                    totalRecommendedSuppliers,
                    totalCreatedPoSuppliers
                });
            }

            return new
            {
                totalRecords,
                page,
                pageSize,
                data
            };
        }

        public async Task<int> CreateFromMprfAsync(int mprfId, string createdBy)
        {
            var header = await _context.PurchasingMprfHeaders
      .Include(x => x.lines)
      .FirstOrDefaultAsync(x => x.mprf_id == mprfId);

            if (header == null)
                throw new Exception("MPRF not found.");

            if (header.status != "REVIEWED")
                throw new Exception("Only REVIEWED MPRF can proceed to canvassing.");

            var approvedLines = header.lines
                .Where(x =>
                    x.item_decision == "APPROVED" &&
                    x.purchasing_qty.HasValue &&
                    x.purchasing_qty.Value > 0)
                .ToList();

            if (!approvedLines.Any())
                throw new Exception("No approved items with purchasing quantity found.");

            var existing = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.MprfId == mprfId);

            if (existing != null)
            {
                if (header.status == "REVIEWED")
                {
                    header.status = "CANVASSING";
                    header.updated_at = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                return existing.CanvassId;
            }
            var canvass = new PurchasingCanvassHeader
            {
                CanvassNo = await GenerateCanvassNoAsync(),
                MprfId = header.mprf_id,
                CanvassDate = DateTime.Now,
                Status = "OPEN",
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.PurchasingCanvassHeaders.Add(canvass);
            await _context.SaveChangesAsync();

            foreach (var line in approvedLines)
            {
                var canvassLine = new PurchasingCanvassLine
                {
                    CanvassId = canvass.CanvassId,
                    MprfLineId = line.mprf_line_id,
                    MaterialId = line.material_id,
                    PurchasingQty = line.purchasing_qty ?? 0,
                    Uom = line.uom,
                    Remarks = line.purchasing_remarks,
                    CreatedAt = DateTime.Now
                };

                _context.PurchasingCanvassLines.Add(canvassLine);
            }

            // MPRF is now in canvassing stage
            header.status = "CANVASSING";
            header.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return canvass.CanvassId;



        }

        public async Task ManualRecommendAsync(int quoteId)
        {
            var quote = await _context.PurchasingCanvassQuotes
                .FirstOrDefaultAsync(x => x.QuoteId == quoteId);

            if (quote == null)
                throw new Exception("Supplier quote not found.");

            var line = await _context.PurchasingCanvassLines
                .FirstOrDefaultAsync(x => x.CanvassLineId == quote.CanvassLineId);

            if (line == null)
                throw new Exception("Canvassing line not found.");

            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == line.CanvassId);

            if (canvass == null)
                throw new Exception("Canvassing record not found.");

            if (canvass.Status == "COMPLETED")
                throw new Exception("Cannot change recommendation after canvassing is completed.");

            var quotes = await _context.PurchasingCanvassQuotes
                .Where(x => x.CanvassLineId == quote.CanvassLineId)
                .ToListAsync();

            foreach (var q in quotes)
            {
                q.IsRecommended = false;
                q.RecommendationReason = null;
                q.UpdatedAt = DateTime.Now;
            }

            quote.IsRecommended = true;
            quote.RecommendationReason = "Manually selected by user.";
            quote.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
        private async Task<string> GenerateCanvassNoAsync()
        {
            var year = DateTime.Now.Year;

            var codes = await _context.PurchasingCanvassHeaders
                .Where(x => x.CanvassNo.StartsWith($"CAN-{year}-"))
                .Select(x => x.CanvassNo)
                .ToListAsync();

            var maxNo = codes
                .Select(x =>
                {
                    var clean = x.Replace($"CAN-{year}-", "");
                    return int.TryParse(clean, out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"CAN-{year}-{maxNo + 1:D4}";
        }

        public async Task<object?> GetByIdAsync(int canvassId)
        {
            var header = await (
                from c in _context.PurchasingCanvassHeaders
                join h in _context.PurchasingMprfHeaders
                    on c.MprfId equals h.mprf_id
                where c.CanvassId == canvassId
                select new
                {
                    c.CanvassId,
                    c.CanvassNo,
                    c.MprfId,
                    MprfNo = h.mprf_no,
                    Department = h.category,
                    c.CanvassDate,
                    c.Status,
                    c.Remarks,
                    c.CreatedBy,
                    c.CreatedAt
                }
            ).FirstOrDefaultAsync();

            if (header == null)
                return null;

            var lines = await (
     from cl in _context.PurchasingCanvassLines
     join ml in _context.Materials
         on cl.MaterialId equals ml.material_id

     join cat in _context.MaterialCategories
         on ml.material_category_id equals cat.material_category_id into catJoin
     from cat in catJoin.DefaultIfEmpty()

     join sub in _context.MaterialSubCategories
         on ml.material_subcategory_id equals sub.material_subcategory_id into subJoin
     from sub in subJoin.DefaultIfEmpty()

     where cl.CanvassId == canvassId
     select new
     {
         cl.CanvassLineId,
         cl.MprfLineId,
         cl.MaterialId,

         MaterialCode = ml.material_code,
         MaterialName = ml.material_name,

         Classification = cat == null
             ? ""
             : sub != null
                 ? cat.category_name + " / " + sub.subcategory_name
                 : cat.category_name,

         cl.PurchasingQty,
         Uom = cl.Uom ?? ml.uom,
         cl.Remarks
     }
 ).ToListAsync();

            var lineIds = lines.Select(x => x.CanvassLineId).ToList();

            var quotes = await (
                from q in _context.PurchasingCanvassQuotes
                join s in _context.Suppliers
                    on q.SupplierId equals s.SupplierId
                join m in _context.Manufacturers
                    on q.ManufacturerId equals m.ManufacturerId into mJoin
                from m in mJoin.DefaultIfEmpty()
                where lineIds.Contains(q.CanvassLineId)
                select new
                {
                    q.QuoteId,
                    q.CanvassLineId,
                    q.SupplierId,
                    s.SupplierName,
                    SupplierAddress = s.Address,
                    q.ManufacturerId,
                    ManufacturerName = m != null ? m.ManufacturerName : null,
                    q.UnitPrice,
                    q.PaymentTerms,
                    q.DeliveryDays,
                    q.CoaAvailable,
                    q.DocumentsRemarks,
                    q.QuotationRef,
                    q.QuoteDate,
                    q.Remarks,
                    q.IsRecommended,
                    q.RecommendationReason
                }
            ).ToListAsync();

            return new
            {
                header,
                lines = lines.Select(l => new
                {
                    l.CanvassLineId,
                    l.MprfLineId,
                    l.MaterialId,
                    l.MaterialCode,
                    l.MaterialName,
                    l.Classification,
                    l.PurchasingQty,
                    l.Uom,
                    l.Remarks,
                    quotes = quotes.Where(q => q.CanvassLineId == l.CanvassLineId).ToList()
                })
            };
        }

        public async Task<int> AddQuoteAsync(CreateCanvassQuoteDto dto)
        {
            var line = await _context.PurchasingCanvassLines
                .FirstOrDefaultAsync(x => x.CanvassLineId == dto.CanvassLineId);

            if (line == null)
                throw new Exception("Canvassing line not found.");

            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == line.CanvassId);

            if (canvass == null)
                throw new Exception("Canvassing header not found.");

            if (canvass.Status == "COMPLETED")
                throw new Exception("Cannot add quote to completed canvassing.");

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.SupplierId == dto.SupplierId && !x.IsDeleted);

            if (supplier == null)
                throw new Exception("Supplier not found.");

            var quote = new PurchasingCanvassQuote
            {
                CanvassLineId = dto.CanvassLineId,
                SupplierId = dto.SupplierId,
                ManufacturerId = dto.ManufacturerId,
                UnitPrice = dto.UnitPrice,
                PaymentTerms = dto.PaymentTerms,
                DeliveryDays = dto.DeliveryDays,
                CoaAvailable = dto.CoaAvailable,
                DocumentsRemarks = dto.DocumentsRemarks,
                QuotationRef = dto.QuotationRef,
                QuoteDate = dto.QuoteDate,
                Remarks = dto.Remarks,
                IsRecommended = false,
                CreatedAt = DateTime.Now
            };

            _context.PurchasingCanvassQuotes.Add(quote);
            await _context.SaveChangesAsync();

            return quote.QuoteId;
        }

        public async Task RecommendAsync(int canvassId)
        {
            var lines = await _context.PurchasingCanvassLines
                .Where(x => x.CanvassId == canvassId)
                .ToListAsync();

            foreach (var line in lines)
            {
                var quotes = await _context.PurchasingCanvassQuotes
                    .Where(x => x.CanvassLineId == line.CanvassLineId)
                    .ToListAsync();

                if (!quotes.Any())
                    continue;

                foreach (var q in quotes)
                {
                    q.IsRecommended = false;
                    q.RecommendationReason = null;
                }

                var recommended = quotes
                    .OrderBy(x => x.UnitPrice)
                    .ThenBy(x => x.DeliveryDays ?? 999)
                    .First();

                recommended.IsRecommended = true;
                recommended.RecommendationReason =
                    "Recommended based on lowest quotation price.";

                recommended.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CompleteAsync(int canvassId)
        {
            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == canvassId);

            if (canvass == null)
                throw new Exception("Canvassing record not found.");

            //if (canvass.Status == "PO_CREATED")
            //    throw new Exception("Canvassing already has PO created.");

            var lines = await _context.PurchasingCanvassLines
                .Where(x => x.CanvassId == canvassId)
                .ToListAsync();

            if (!lines.Any())
                throw new Exception("No canvassing lines found.");

            var lineIds = lines.Select(x => x.CanvassLineId).ToList();

            var hasMissingRecommendation = await _context.PurchasingCanvassQuotes
                .Where(x => lineIds.Contains(x.CanvassLineId))
                .GroupBy(x => x.CanvassLineId)
                .AnyAsync(g => !g.Any(q => q.IsRecommended));

            if (hasMissingRecommendation)
                throw new Exception("All materials must have one recommended supplier before completing canvassing.");

            canvass.Status = "COMPLETED";
            canvass.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<object> GetLinkedSuppliersByMaterialAsync(int materialId)
        {
            var suppliers = await (
                from sm in _context.SupplierMaterials
                join s in _context.Suppliers
                    on sm.SupplierId equals s.SupplierId
                join m in _context.Manufacturers
                    on sm.ManufacturerId equals m.ManufacturerId into mJoin
                from m in mJoin.DefaultIfEmpty()
                where sm.MaterialId == materialId
                      && sm.IsActive
                      && !sm.IsDeleted
                      && s.IsActive
                      && !s.IsDeleted
                orderby sm.IsPreferred descending, s.SupplierName
                select new
                {
                    sm.SupplierMaterialId,
                    sm.SupplierId,
                    s.SupplierCode,
                    s.SupplierName,
                    s.PaymentTerms,
                    s.LeadTimeDays,
                    sm.ManufacturerId,
                    ManufacturerName = m != null ? m.ManufacturerName : null,
                    AccreditationStatus = m != null ? m.AccreditationStatus : null,
                    CoaRequired = m != null ? m.CoaRequired : null,
                    sm.IsPreferred,
                    sm.Remarks
                }
            ).ToListAsync();

            return suppliers;
        }

        public async Task<bool> UpdateQuoteAsync(int quoteId, UpdateCanvassQuoteDto dto)
        {
            var quote = await _context.PurchasingCanvassQuotes
                .FirstOrDefaultAsync(x => x.QuoteId == quoteId);

            if (quote == null)
                return false;

            var line = await _context.PurchasingCanvassLines
                .FirstOrDefaultAsync(x => x.CanvassLineId == quote.CanvassLineId);

            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == line!.CanvassId);

            if (canvass == null)
                throw new Exception("Canvassing header not found.");

            if (canvass.Status == "COMPLETED")
                throw new Exception("Cannot update quote after canvassing is completed.");

            quote.SupplierId = dto.SupplierId;
            quote.ManufacturerId = dto.ManufacturerId;
            quote.UnitPrice = dto.UnitPrice;
            quote.PaymentTerms = dto.PaymentTerms;
            quote.DeliveryDays = dto.DeliveryDays;
            quote.CoaAvailable = dto.CoaAvailable;
            quote.DocumentsRemarks = dto.DocumentsRemarks;
            quote.QuotationRef = dto.QuotationRef;
            quote.QuoteDate = dto.QuoteDate;
            quote.Remarks = dto.Remarks;

            quote.IsRecommended = false;
            quote.RecommendationReason = null;
            quote.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteQuoteAsync(int quoteId)
        {
            var quote = await _context.PurchasingCanvassQuotes
                .FirstOrDefaultAsync(x => x.QuoteId == quoteId);

            if (quote == null)
                return false;

            var line = await _context.PurchasingCanvassLines
                .FirstOrDefaultAsync(x => x.CanvassLineId == quote.CanvassLineId);

            var canvass = await _context.PurchasingCanvassHeaders
                .FirstOrDefaultAsync(x => x.CanvassId == line!.CanvassId);

            if (canvass == null)
                throw new Exception("Canvassing header not found.");

            if (canvass.Status == "COMPLETED")
                throw new Exception("Cannot delete quote after canvassing is completed.");

            _context.PurchasingCanvassQuotes.Remove(quote);
            await _context.SaveChangesAsync();

            return true;
        }



    }

}