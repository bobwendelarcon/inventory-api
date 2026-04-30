using inventory_api.Data;
using inventory_api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class InventoryDisplayService
    {
        private readonly AppDbContext _context;

        public InventoryDisplayService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, object>> GetAllAsync(
            int page = 1,
            int pageSize = 30,
            string lot_no = "",
            string product = "",
            string warehouse = "",
            string stockStatus = "",
            string expiryStatus = "",
            string months = "",
            string from = "",
            string to = "",
            string order = "desc"
        )
        {
            TimeZoneInfo phTimeZone;

            try
            {
                phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            }
            catch
            {
                phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }

            var todayPh = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;

            var query =
                from lot in _context.ProductLotNumbers
                join productData in _context.Products
                    on lot.product_id equals productData.product_id into productJoin
                from productData in productJoin.DefaultIfEmpty()

                join branch in _context.Branches
                    on lot.branch_id equals branch.branch_id into branchJoin
                from branch in branchJoin.DefaultIfEmpty()

                where !lot.is_deleted
                //select new
                //{
                //    product_id = lot.product_id,
                //    description = productData != null ? (productData.product_name ?? "") : "",
                //    uom = productData != null ? (productData.uom ?? "") : "",
                //    pack_qty = productData != null ? (int)(productData.pack_qty ?? 0) : 0,
                //    pack_uom = productData != null ? (productData.pack_uom ?? "") : "",
                //    lot_no = lot.lot_no ?? "",
                //    warehouse = branch != null ? (branch.branch_name ?? "") : lot.branch_id,
                //    qty = (int)lot.quantity,
                //    created_at = lot.created_at,
                //    manufacturing_date = lot.manufacturing_date,
                //    expiration_date = lot.expiration_date
                //};
                select new
                {
                    product_id = lot.product_id,
                    branch_id = lot.branch_id,
                    description = productData != null ? (productData.product_name ?? "") : "",
                    uom = productData != null ? (productData.uom ?? "") : "",
                    pack_qty = productData != null ? (decimal)(productData.pack_qty ?? 0) : 0,
                    pack_uom = productData != null ? (productData.pack_uom ?? "") : "",
                    lot_no = lot.lot_no ?? "",
                    warehouse = branch != null ? (branch.branch_name ?? "") : lot.branch_id,
                    qty = (decimal)lot.quantity,
                    created_at = lot.created_at,
                    manufacturing_date = lot.manufacturing_date,
                    expiration_date = lot.expiration_date
                };

            if (!string.IsNullOrWhiteSpace(lot_no))
                query = query.Where(x => x.lot_no.Contains(lot_no));

            if (!string.IsNullOrWhiteSpace(product))
                query = query.Where(x => x.description.Contains(product));

            if (!string.IsNullOrWhiteSpace(warehouse))
                query = query.Where(x => x.branch_id == warehouse);

            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
            {
                var fromDateOnly = fromDate.Date;
                query = query.Where(x => x.created_at.Date >= fromDateOnly);
            }

            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
            {
                var toDateOnly = toDate.Date.AddDays(1);
                query = query.Where(x => x.created_at.Date < toDateOnly);
            }

            if (stockStatus == "zero")
                query = query.Where(x => x.qty <= 0);
            else if (stockStatus == "low")
                query = query.Where(x => x.qty > 0 && x.qty <= 5);
            else if (stockStatus == "normal")
                query = query.Where(x => x.qty > 5 && x.qty <= 100);
            else if (stockStatus == "over")
                query = query.Where(x => x.qty > 100);

            if (expiryStatus == "expired")
            {
                query = query.Where(x =>
                    x.expiration_date.HasValue &&
                    x.expiration_date.Value.Date < todayPh);
            }
            else if (expiryStatus == "notexpired")
            {
                query = query.Where(x =>
                    x.expiration_date.HasValue &&
                    x.expiration_date.Value.Date >= todayPh);
            }
            else if (expiryStatus == "available")
            {
                query = query.Where(x =>
                    x.qty > 0 &&
                    x.expiration_date.HasValue &&
                    x.expiration_date.Value.Date >= todayPh);
            }
            else if (expiryStatus == "near")
            {
                query = query.Where(x =>
                    x.expiration_date.HasValue &&
                    x.expiration_date.Value.Date >= todayPh &&
                    x.expiration_date.Value.Date <= todayPh.AddMonths(2));
            }
            else if (expiryStatus == "safe")
            {
                query = query.Where(x =>
                    x.expiration_date.HasValue &&
                    x.expiration_date.Value.Date > todayPh.AddMonths(2));
            }
            else if (expiryStatus == "noexp")
            {
                query = query.Where(x => !x.expiration_date.HasValue);
            }

            //if (!string.IsNullOrWhiteSpace(months) && int.TryParse(months, out var m))
            //{
            //    query = query.Where(x =>
            //        x.expiration_date.HasValue &&
            //        x.expiration_date.Value.Date >= todayPh &&
            //        x.expiration_date.Value.Date <= todayPh.AddMonths(m));
            //}

            if (!string.IsNullOrWhiteSpace(months))
            {
                if (months == "over12")
                {
                    query = query.Where(x =>
                        x.expiration_date.HasValue &&
                        x.expiration_date.Value.Date > todayPh.AddMonths(12));
                }
                else if (int.TryParse(months, out var m))
                {
                    var endDate = todayPh.AddMonths(m);

                    query = query.Where(x =>
                        x.expiration_date.HasValue &&
                        x.expiration_date.Value.Date >= todayPh &&
                        x.expiration_date.Value.Date <= endDate);
                }
            }

            query = order?.ToLower() == "asc"
                ? query.OrderBy(x => x.lot_no)
                : query.OrderByDescending(x => x.lot_no);

            var total = await query.CountAsync();

            var rawResult = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = rawResult.Select(x => new InventoryDisplayDto
            {
                product_id = x.product_id,
                branch_id = x.branch_id,
                description = x.description,
                uom = x.uom,
                pack_qty = x.pack_qty,
                pack_uom = x.pack_uom,
                lot_no = x.lot_no,
                warehouse = x.warehouse,
                qty = x.qty,
                date = ConvertToPhilippineTime(x.created_at, phTimeZone).ToString("yyyy-MM-dd"),
                manufacturing_date = x.manufacturing_date.HasValue
          ? ConvertToPhilippineTime(x.manufacturing_date.Value, phTimeZone).ToString("yyyy-MM-dd")
          : "",
                expiration_date = x.expiration_date.HasValue
          ? ConvertToPhilippineTime(x.expiration_date.Value, phTimeZone).ToString("yyyy-MM-dd")
          : ""
            }).ToList();

            return new Dictionary<string, object>
            {
                { "data", result },
                { "total", total },
                { "page", page },
                { "pageSize", pageSize }
            };
        }



        private static DateTime ConvertToPhilippineTime(DateTime dateTime, TimeZoneInfo phTimeZone)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return TimeZoneInfo.ConvertTimeFromUtc(dateTime, phTimeZone);

            if (dateTime.Kind == DateTimeKind.Local)
                return TimeZoneInfo.ConvertTime(dateTime, phTimeZone);

            return dateTime;
        }
    }
}