using inventory_api.Data;
using inventory_api.DTOs.Purchasing;
using inventory_api.Models.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing
{
    public class MprfService
    {
        private readonly AppDbContext _context;



        public MprfService(AppDbContext context)
        {
            _context = context;
        }

        //Creator can only see the request
        //public async Task<List<object>> GetAllAsync(string userId)
        //{
        //    var data = await _context.PurchasingMprfHeaders
        //        .Where(h => h.requested_by == userId)
        //        .OrderByDescending(h => h.mprf_id)
        //        .Select(h => new
        //        {
        //            h.mprf_id,
        //            h.mprf_no,
        //            h.category,
        //            h.request_date,
        //            h.week,
        //            h.requested_by,

        //            requested_by_name = _context.Users
        //                .Where(u => u.user_id == h.requested_by)
        //                .Select(u => string.IsNullOrWhiteSpace(u.full_name)
        //                    ? u.username
        //                    : u.full_name)
        //                .FirstOrDefault(),

        //            h.status,

        //            can_edit =
        //                h.requested_by == userId &&
        //                (h.status == "DRAFT"
        //                 || h.status == "RETURNED"),

        //            h.created_at,
        //            h.updated_at
        //        })
        //        .Cast<object>()
        //        .ToListAsync();

        //    return data;
        //}

      //  all can see the request
        public async Task<List<object>> GetAllAsync(string userId)
        {
            var data = await _context.PurchasingMprfHeaders
                .OrderByDescending(h => h.mprf_id)
                .Select(h => new
                {
                    h.mprf_id,
                    h.mprf_no,
                    h.category,
                    h.request_date,
                    h.week,
                    h.requested_by,

                    requested_by_name = _context.Users
                        .Where(u => u.user_id == h.requested_by)
                        .Select(u =>
                            string.IsNullOrWhiteSpace(u.full_name)
                                ? u.username
                                : u.full_name)
                        .FirstOrDefault(),

                    h.status,

                    can_edit =
                        h.requested_by == userId
                        && (h.status == "DRAFT"
                            || h.status == "RETURNED"),

                    h.created_at,
                    h.updated_at
                })
                .Cast<object>()
                .ToListAsync();

            return data;
        }

        public async Task<object?> GetByIdAsync(int id)
        {
            var data = await _context.PurchasingMprfHeaders
                .Where(h => h.mprf_id == id)
                .Select(h => new
                {
                    h.mprf_id,
                    h.mprf_no,
                    h.category,
                    h.request_date,
                    h.week,
                    h.requested_by,
                    h.review_decision,
                    h.review_remarks,
                    h.reviewed_at,

                    requested_by_name = _context.Users
                        .Where(u => u.user_id == h.requested_by)
                        .Select(u => string.IsNullOrWhiteSpace(u.full_name)
                            ? u.username
                            : u.full_name)
                        .FirstOrDefault(),

                    h.status,
                    h.created_at,
                    h.updated_at,

                    lines = h.lines.Select(l => new
                    {
                        l.mprf_line_id,
                        l.material_id,
                        l.purchasing_qty,
                        l.purchasing_remarks,
                        l.item_decision,

                        material_code = _context.Materials
                            .Where(m => m.material_id == l.material_id)
                            .Select(m => m.material_code)
                            .FirstOrDefault(),

                        material_name = _context.Materials
                            .Where(m => m.material_id == l.material_id)
                            .Select(m => m.material_name)
                            .FirstOrDefault(),

                        category_name = _context.Materials
                            .Where(m => m.material_id == l.material_id)
                            .Select(m => m.Category.category_name)
                            .FirstOrDefault(),

                        subcategory_name = _context.Materials
                            .Where(m => m.material_id == l.material_id)
                            .Select(m => m.SubCategory.subcategory_name)
                            .FirstOrDefault(),

                        l.qty_on_hand,
                        l.uom,
                        l.remarks
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (data == null)
                return null;

            var relatedPurchaseOrders = await (
        from c in _context.PurchasingCanvassHeaders
        join po in _context.PurchaseOrderHeaders
            on c.CanvassId equals po.CanvassId
        join s in _context.Suppliers
            on po.SupplierId equals s.SupplierId
        where c.MprfId == id
        orderby po.PoId descending
        select new
        {
            poId = po.PoId,
            poNo = po.PoNo,
            printedPoNo = po.PrintedPoNo,
            supplierName = s.SupplierName,
            poDate = po.PoDate,
            status = po.Status,
            totalAmount = po.TotalAmount
        }
    ).ToListAsync();

            return new
            {
                data.mprf_id,
                data.mprf_no,
                data.category,
                data.request_date,
                data.week,
                data.requested_by,
                data.requested_by_name,
                data.review_decision,
                data.review_remarks,
                data.reviewed_at,
                data.status,
                data.created_at,
                data.updated_at,
                data.lines,
                related_purchase_orders = relatedPurchaseOrders

                //po_id = poInfo != null ? poInfo.PoId : (int?)null,
                //po_no = poInfo != null ? poInfo.PoNo : null,
                //po_status = poInfo != null ? poInfo.Status : null,
                //po_total_amount = poInfo != null ? poInfo.TotalAmount : (decimal?)null
            };
        }

        public async Task<int> CreateAsync(CreateMprfDto dto)
        {
            if (dto.lines == null || dto.lines.Count == 0)
                throw new Exception("Please add at least one material line.");

            var mprfNo = string.IsNullOrWhiteSpace(dto.mprf_no)
      ? await GenerateMprfNoAsync()
      : dto.mprf_no.Trim();

            bool exists = await _context.PurchasingMprfHeaders
                .AnyAsync(x => x.mprf_no == mprfNo);

            if (exists)
                throw new Exception("MPRF No already exists.");

            var header = new MprfHeader
            {
                mprf_no = mprfNo,
                category = dto.category,
                request_date = dto.request_date == default ? DateTime.Today : dto.request_date,
                week = dto.week,
                requested_by = dto.requested_by,
                status = "DRAFT",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                lines = dto.lines.Select(x => new MprfLine
                {
                    material_id = x.material_id,
                    qty_on_hand = x.qty_on_hand,
                    requested_qty = x.requested_qty,
                    uom = x.uom,
                    remarks = x.remarks,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                }).ToList()
            };

            _context.PurchasingMprfHeaders.Add(header);
            await _context.SaveChangesAsync();

            return header.mprf_id;
        }

        public async Task<bool> SubmitAsync(int id)
        {
            var header = await _context.PurchasingMprfHeaders
                .FirstOrDefaultAsync(x => x.mprf_id == id);

            if (header == null)
                return false;

            if (header.status != "DRAFT"
      && header.status != "RETURNED")
            {
                throw new Exception("Only DRAFT or RETURNED MPRF can be submitted.");
            }

            header.status = "SUBMITTED";
            header.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var header = await _context.PurchasingMprfHeaders
                .FirstOrDefaultAsync(x => x.mprf_id == id);

            if (header == null)
                return false;

            if (header.status != "DRAFT"
     && header.status != "RETURNED")
                throw new Exception("Only DRAFT MPRF can be deleted.");

            _context.PurchasingMprfHeaders.Remove(header);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<string> GenerateMprfNoAsync()
        {
            var list = await _context.PurchasingMprfHeaders
                .Select(x => x.mprf_no)
                .ToListAsync();

            var maxNo = list
                .Select(x => int.TryParse(x, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            return (maxNo + 1).ToString();
        }

        // UPDATE

        public async Task<bool> UpdateAsync(int id, UpdateMprfDto dto, string userId)
        {
            var header = await _context.PurchasingMprfHeaders
                .Include(x => x.lines)
                .FirstOrDefaultAsync(x => x.mprf_id == id);

            if (header == null)
                return false;

            if (header.status != "DRAFT" && header.status != "RETURNED")
                throw new Exception("Only DRAFT or RETURNED MPRF can be edited.");

            if (header.requested_by != userId)
                throw new Exception("Only the creator can edit this MPRF.");

            if (dto.lines == null || dto.lines.Count == 0)
                throw new Exception("Please add at least one material line.");

            var mprfNo = string.IsNullOrWhiteSpace(dto.mprf_no)
                ? header.mprf_no
                : dto.mprf_no.Trim();

            bool exists = await _context.PurchasingMprfHeaders
                .AnyAsync(x => x.mprf_no == mprfNo && x.mprf_id != id);

            if (exists)
                throw new Exception("MPRF No already exists.");

            header.mprf_no = mprfNo;
            header.category = dto.category;
            header.request_date = dto.request_date == default ? DateTime.Today : dto.request_date;
            header.week = dto.week;
            header.updated_at = DateTime.UtcNow;

            _context.PurchasingMprfLines.RemoveRange(header.lines);

            header.lines = dto.lines.Select(x => new MprfLine
            {
                mprf_id = id,
                material_id = x.material_id,
                qty_on_hand = x.qty_on_hand,
                requested_qty = x.requested_qty,
                uom = x.uom,
                remarks = x.remarks,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            }).ToList();

            await _context.SaveChangesAsync();
            return true;
        }




        //review of request

        public async Task<List<object>> GetReviewListAsync()
        {
            var data = await _context.PurchasingMprfHeaders
                .Where(h => h.status == "SUBMITTED"
         || h.status == "REVIEWED"
         || h.status == "CANVASSING"
         || h.status == "PO_CREATED")
                .OrderByDescending(h => h.mprf_id)
                .Select(h => new
                {
                    h.mprf_id,
                    h.mprf_no,
                    h.category,
                    h.request_date,
                    h.week,
                    h.requested_by,

                    requested_by_name = _context.Users
                        .Where(u => u.user_id == h.requested_by)
                        .Select(u => string.IsNullOrWhiteSpace(u.full_name)
                            ? u.username
                            : u.full_name)
                        .FirstOrDefault(),

                    h.reviewed_by,
                    reviewed_by_name = _context.Users
                        .Where(u => u.user_id == h.reviewed_by)
                        .Select(u => string.IsNullOrWhiteSpace(u.full_name)
                            ? u.username
                            : u.full_name)
                        .FirstOrDefault(),

                    h.review_decision,
                    h.review_remarks,
                    h.reviewed_at,
                    h.status,
                    h.created_at,
                    h.updated_at
                })
                .ToListAsync();

            var result = new List<object>();

            foreach (var h in data)
            {
                var totalRecommendedSuppliers = await (
                    from c in _context.PurchasingCanvassHeaders
                    join cl in _context.PurchasingCanvassLines
                        on c.CanvassId equals cl.CanvassId
                    join q in _context.PurchasingCanvassQuotes
                        on cl.CanvassLineId equals q.CanvassLineId
                    where c.MprfId == h.mprf_id
                          && q.IsRecommended
                    select q.SupplierId
                ).Distinct().CountAsync();

                var totalCreatedPoSuppliers = await (
                    from c in _context.PurchasingCanvassHeaders
                    join po in _context.PurchaseOrderHeaders
                        on c.CanvassId equals po.CanvassId
                    where c.MprfId == h.mprf_id
                          && po.Status != "CANCELLED"
                    select po.SupplierId
                ).Distinct().CountAsync();

                result.Add(new
                {
                    h.mprf_id,
                    h.mprf_no,
                    h.category,
                    h.request_date,
                    h.week,
                    h.requested_by,
                    h.requested_by_name,
                    h.reviewed_by,
                    h.reviewed_by_name,
                    h.review_decision,
                    h.review_remarks,
                    h.reviewed_at,
                    h.status,
                    h.created_at,
                    h.updated_at,

                    total_recommended_suppliers = totalRecommendedSuppliers,
                    total_created_po_suppliers = totalCreatedPoSuppliers
                });
            }

            return result;
        }

        public async Task<bool> ReviewAsync(int id, ReviewMprfDto dto)
        {
            var header = await _context.PurchasingMprfHeaders
                .Include(x => x.lines)
                .FirstOrDefaultAsync(x => x.mprf_id == id);

            if (header == null)
                return false;

            if (header.status != "SUBMITTED")
                throw new Exception("Only SUBMITTED MPRF can be reviewed.");

            if (string.IsNullOrWhiteSpace(dto.reviewed_by))
                throw new Exception("Reviewed by is required.");

            if (string.IsNullOrWhiteSpace(dto.review_decision))
                throw new Exception("Review decision is required.");

            foreach (var lineDto in dto.lines)
            {
                var line = header.lines.FirstOrDefault(x => x.mprf_line_id == lineDto.mprf_line_id);

                if (line == null)
                    continue;

                line.purchasing_qty = lineDto.purchasing_qty;

                line.purchasing_remarks = lineDto.purchasing_remarks;
                line.item_decision = string.IsNullOrWhiteSpace(lineDto.item_decision)
    ? "APPROVED"
    : lineDto.item_decision;
                line.updated_at = DateTime.UtcNow;
            }

            header.reviewed_by = dto.reviewed_by;
            header.review_decision = dto.review_decision;
            header.review_remarks = dto.review_remarks;
            header.reviewed_at = DateTime.UtcNow;
            header.updated_at = DateTime.UtcNow;

            if (dto.review_decision == "APPROVE_FOR_CANVASSING")
                header.status = "REVIEWED";
            else if (dto.review_decision == "RETURN_TO_REQUESTOR")
                header.status = "RETURNED";
            else if (dto.review_decision == "REJECT")
                header.status = "REJECTED";

            await _context.SaveChangesAsync();
            return true;
        }


    }
}