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
     string productId = "",
     string lot_no = "",
     string search = "",
     string warehouse = "",
     string category = "",
     string stockStatus = "",
     string expiryStatus = "",
     string months = "",
     string from = "",
     string to = "",
     string sortBy = "lot",
     string order = "desc")
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

                join categoryData in _context.Categories
     on productData.catg_id equals categoryData.catg_id into categoryJoin
                from categoryData in categoryJoin.DefaultIfEmpty()

                join branch in _context.Branches
                    on lot.branch_id equals branch.branch_id into branchJoin
                from branch in branchJoin.DefaultIfEmpty()

                where !lot.is_deleted && !productData.is_deleted

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

                    category_name = categoryData != null
          ? (categoryData.catg_name ?? "")
          : "",

                    branch_id = lot.branch_id,

                    description = productData != null
          ? (productData.product_name ?? "")
          : "",

                    product_description = productData != null
          ? (productData.product_description ?? "")
          : "",

                    uom = productData != null ? (productData.uom ?? "") : "",
                    pack_qty = productData != null ? (decimal)(productData.pack_qty ?? 0) : 0,
                    pack_uom = productData != null ? (productData.pack_uom ?? "") : "",
                    stock_level = productData != null
    ? productData.stock_level
    : 0,
                    lot_no = lot.lot_no ?? "",
                    warehouse = branch != null ? (branch.branch_name ?? "") : lot.branch_id,
                    qty = (decimal)lot.quantity,
                    created_at = lot.created_at,
                    manufacturing_date = lot.manufacturing_date,
                    expiration_date = lot.expiration_date
                };


            if (!string.IsNullOrWhiteSpace(productId))
            {
                var selectedProductId = productId.Trim();

                query = query.Where(x =>
                    x.product_id == selectedProductId
                );
            }

            if (!string.IsNullOrWhiteSpace(lot_no))
            {
                query = query.Where(x =>
                    x.lot_no.Contains(lot_no)
                );
            }



            //if (!string.IsNullOrWhiteSpace(lot_no))
            //    query = query.Where(x => x.lot_no.Contains(lot_no));

            //if (!string.IsNullOrWhiteSpace(product))
            //    query = query.Where(x => x.description.Contains(product));
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(x =>
                    x.description.Contains(keyword) ||
                    x.product_description.Contains(keyword)
                );
            }

            if (!string.IsNullOrWhiteSpace(warehouse))
                query = query.Where(x => x.branch_id == warehouse);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(x => x.category_name == category);

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

            if (stockStatus == "available")
            {
                // Show all inventory except zero or negative stock
                query = query.Where(x => x.qty > 0);
            }
            else if (stockStatus == "zero")
            {
                query = query.Where(x => x.qty <= 0);
            }
            else if (stockStatus == "low")
            {
                query = query.Where(x =>
                    x.qty > 0 &&
                    x.stock_level > 0 &&
                    x.qty < x.stock_level
                );
            }
            else if (stockStatus == "normal")
            {
                query = query.Where(x =>
                    x.qty > 0 &&
                    (
                        x.stock_level <= 0 ||
                        x.qty >= x.stock_level
                    )
                );
            }

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

            var sort = sortBy?.ToLower();
            var dir = order?.ToLower();

            query = sort switch
            {
                "generic" => dir == "asc"
                    ? query.OrderBy(x => x.description)
                           .ThenBy(x => x.lot_no)
                    : query.OrderByDescending(x => x.description)
                           .ThenByDescending(x => x.lot_no),

                "brand" => dir == "asc"
                    ? query.OrderBy(x => x.product_description)
                           .ThenBy(x => x.description)
                           .ThenBy(x => x.lot_no)
                    : query.OrderByDescending(x => x.product_description)
                           .ThenByDescending(x => x.description)
                           .ThenByDescending(x => x.lot_no),

                _ => dir == "asc"
                    ? query.OrderBy(x => x.lot_no)
                    : query.OrderByDescending(x => x.lot_no)
            };

            var total = await query.CountAsync();

            var rawResult = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var allocations = await (
    from a in _context.DailyOrderAllocations
    join line in _context.DailyOrderLines
        on a.order_line_id equals line.order_line_id
    join header in _context.DailyOrderHeaders
        on line.order_id equals header.order_id
    where !header.is_deleted
          && a.allocated_qty > 0
    select new
    {
        a.product_id,
        a.branch_id,
        a.lot_no,
        a.allocated_qty,

        header.order_no,
        header.customer_name
    }
).ToListAsync();

            var result = rawResult.Select(x =>
            {
                var reservedList = allocations
    .Where(a =>
        (a.product_id ?? "").Trim() == (x.product_id ?? "").Trim() &&
        (a.branch_id ?? "").Trim() == (x.branch_id ?? "").Trim() &&
        (a.lot_no ?? "").Trim() == (x.lot_no ?? "").Trim())
    .ToList();

                var reservedQty = reservedList.Sum(a => a.allocated_qty);

                var availableQty = Math.Max(0, x.qty - reservedQty);

                return new InventoryDisplayDto
                {
                    product_id = x.product_id,
                    branch_id = x.branch_id,
                    category_name = x.category_name,
                    description = x.description,
                    product_description = x.product_description,
                    uom = x.uom,
                    pack_qty = x.pack_qty,
                    stock_level = x.stock_level,
                    pack_uom = x.pack_uom,
                    lot_no = x.lot_no,
                    warehouse = x.warehouse,

                    qty = x.qty,
                    reserved_qty = reservedQty,
                    available_qty = availableQty,

                    reserved_details = reservedList
    .GroupBy(r => new { r.order_no, r.customer_name })
    .Select(g => new InventoryReservedDetailDto
    {
        order_no = g.Key.order_no,
        customer_name = g.Key.customer_name,
        reserved_qty = g.Sum(x => x.allocated_qty)
    })
    .ToList(),


                    date = ConvertToPhilippineTime(
                        x.created_at,
                        phTimeZone
                    ).ToString("yyyy-MM-dd"),




                    manufacturing_date = x.manufacturing_date.HasValue
                        ? ConvertToPhilippineTime(
                            x.manufacturing_date.Value,
                            phTimeZone
                          ).ToString("yyyy-MM-dd")
                        : "",

                    expiration_date = x.expiration_date.HasValue
                        ? ConvertToPhilippineTime(
                            x.expiration_date.Value,
                            phTimeZone
                          ).ToString("yyyy-MM-dd")
                        : ""
                };
            }).ToList();

            return new Dictionary<string, object>
            {
                { "data", result },
                { "total", total },
                { "page", page },
                { "pageSize", pageSize }
            };
        }



        public async Task<Dictionary<string, object>> GetInventoryAgingAsync(
    int page = 1,
    int pageSize = 30,
    string search = "",
    string lotNo = "",
    string category = "",
    string warehouse = "",
    string status = "",
    int? minimumDays = null,
    int? maximumDays = null,
    string order = "desc")
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 10, 100);

            TimeZoneInfo phTimeZone;

            try
            {
                phTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            }
            catch
            {
                phTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Singapore Standard Time"
                    );
            }

            var todayPh =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    phTimeZone
                ).Date;

            var movementQuery =
                from transaction in _context.InventoryTransactions
                where !transaction.is_deleted
                group transaction by new
                {
                    transaction.product_id,
                    transaction.branch_id,
                    transaction.lot_no
                }
                into movement
                select new
                {
                    movement.Key.product_id,
                    movement.Key.branch_id,
                    movement.Key.lot_no,

                    date_in = movement
                        .Where(x =>
                            x.transaction_type == "IN")
                        .Min(x => (DateTime?)x.created_at),

                    last_out_date = movement
                        .Where(x =>
                            x.transaction_type == "OUT")
                        .Max(x => (DateTime?)x.created_at)
                };


            var verificationQuery =
    from transaction in _context.InventoryTransactions

    where
        !transaction.is_deleted &&
        transaction.reference_type == "INVENTORY_CLEANUP"

    group transaction by new
    {
        transaction.product_id,
        transaction.branch_id,
        transaction.lot_no
    }
    into verification

    select new
    {
        verification.Key.product_id,
        verification.Key.branch_id,
        verification.Key.lot_no,

        last_verified_at =
            verification.Max(x =>
                (DateTime?)x.created_at)
    };

            var query =
       from lot in _context.ProductLotNumbers

       join product in _context.Products
           on lot.product_id equals product.product_id

       join categoryData in _context.Categories
           on product.catg_id equals categoryData.catg_id
           into categoryJoin

       from categoryData in categoryJoin.DefaultIfEmpty()

       join branch in _context.Branches
           on lot.branch_id equals branch.branch_id
           into branchJoin

       from branch in branchJoin.DefaultIfEmpty()

       join movement in movementQuery
           on new
           {
               lot.product_id,
               lot.branch_id,
               lot.lot_no
           }
           equals new
           {
               movement.product_id,
               movement.branch_id,
               movement.lot_no
           }
           into movementJoin

       from movement in movementJoin.DefaultIfEmpty()

       join verification in verificationQuery
    on new
    {
        lot.product_id,
        lot.branch_id,
        lot.lot_no
    }
    equals new
    {
        verification.product_id,
        verification.branch_id,
        verification.lot_no
    }
    into verificationJoin

       from verification in verificationJoin.DefaultIfEmpty()

       where
           !lot.is_deleted &&
           !product.is_deleted &&
           lot.quantity > 0

       select new
       {
           lot.product_id,
           lot.branch_id,

           category_id =
               product.catg_id ?? "",

           category_name =
               categoryData != null
                   ? categoryData.catg_name ?? ""
                   : "",

           product_name =
               product.product_name ?? "",

           product_description =
               product.product_description ?? "",

           lot_no =
               lot.lot_no ?? "",

           warehouse =
               branch != null
                   ? branch.branch_name ?? lot.branch_id
                   : lot.branch_id,

           qty =
               (decimal)lot.quantity,

           uom =
               product.uom ?? "",

           date_in =
               movement != null
                   ? movement.date_in
                   : null,

           last_out_date =
               movement != null
                   ? movement.last_out_date
                   : null,

           last_verified_at =
    verification != null
        ? verification.last_verified_at
        : null,

           manufacturing_date =
               lot.manufacturing_date,

           expiration_date =
               lot.expiration_date
       };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(x =>
                    x.product_name.Contains(keyword) ||
                    x.product_description.Contains(keyword) ||
                    x.lot_no.Contains(keyword)
                );
            }

            if (!string.IsNullOrWhiteSpace(lotNo))
            {
                var lotKeyword = lotNo.Trim();

                query = query.Where(x =>
                    x.lot_no.Contains(lotKeyword)
                );
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                var selectedCategory = category.Trim();

                query = query.Where(x =>
                    x.category_id == selectedCategory
                );
            }


            if (!string.IsNullOrWhiteSpace(warehouse))
            {
                query = query.Where(x =>
                    x.branch_id == warehouse
                );
            }

            var rawRows = await query.ToListAsync();

            var rows = rawRows
                .Select(x =>
                {
                    DateTime? dateInPh = x.date_in.HasValue
                        ? ConvertToPhilippineTime(
                            x.date_in.Value,
                            phTimeZone
                        )
                        : null;

                    DateTime? lastOutPh = x.last_out_date.HasValue
                        ? ConvertToPhilippineTime(
                            x.last_out_date.Value,
                            phTimeZone
                        )
                        : null;

                    DateTime? lastVerifiedPh =
    x.last_verified_at.HasValue
        ? ConvertToPhilippineTime(
            x.last_verified_at.Value,
            phTimeZone
        )
        : null;

                    DateTime? expirationDate =
                        x.expiration_date?.Date;

                    int daysInInventory = dateInPh.HasValue
                        ? Math.Max(
                            0,
                            (todayPh - dateInPh.Value.Date).Days
                        )
                        : 0;

                    int? daysSinceLastOut = lastOutPh.HasValue
                        ? Math.Max(
                            0,
                            (todayPh - lastOutPh.Value.Date).Days
                        )
                        : null;

                    int? daysToExpiry = expirationDate.HasValue
                        ? (expirationDate.Value - todayPh).Days
                        : null;

                    string agingStatus;

                    if (daysToExpiry.HasValue &&
                        daysToExpiry.Value < 0)
                    {
                        agingStatus = "EXPIRED";
                    }
                    else if (daysToExpiry.HasValue &&
                             daysToExpiry.Value <= 60)
                    {
                        agingStatus = "NEAR_EXPIRY";
                    }
                    else if (!dateInPh.HasValue)
                    {
                        agingStatus = "TRANSACTION_ISSUE";
                    }
                    else if (daysInInventory > 14)
                    {
                        agingStatus = "CLEANUP_NEEDED";
                    }
                    else if (daysInInventory > 7)
                    {
                        agingStatus = "AGING";
                    }
                    else
                    {
                        agingStatus = "HEALTHY";
                    }

                    bool wasVerified =
                        lastVerifiedPh.HasValue;

                    bool needsVerification =
                        !wasVerified &&
                        (
                            agingStatus == "CLEANUP_NEEDED" ||
                            agingStatus == "EXPIRED" ||
                            agingStatus == "TRANSACTION_ISSUE"
                        );

                    return new InventoryAgingDto
                    {
                        product_id = x.product_id,
                        branch_id = x.branch_id,

                        product_name = x.product_name,
                        product_description =
                            x.product_description,

                        lot_no = x.lot_no,
                        warehouse = x.warehouse,

                        qty = x.qty,
                        uom = x.uom,

                        date_in = dateInPh,
                        last_out_date = lastOutPh,

                        last_verified_at = lastVerifiedPh,

                        manufacturing_date =
                            x.manufacturing_date,

                        expiration_date =
                            x.expiration_date,

                        days_in_inventory =
                            daysInInventory,

                        days_since_last_out =
                            daysSinceLastOut,

                        days_to_expiry =
                            daysToExpiry,

                        aging_status =
                            agingStatus,

                        needs_verification =
                            needsVerification
                    };
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus =
                    status.Trim().ToUpper();

                rows = rows
                    .Where(x =>
                        x.aging_status == normalizedStatus)
                    .ToList();
            }

            if (minimumDays.HasValue)
            {
                rows = rows
                    .Where(x =>
                        x.days_in_inventory >= minimumDays.Value)
                    .ToList();
            }

            if (maximumDays.HasValue)
            {
                rows = rows
                    .Where(x =>
                        x.days_in_inventory <= maximumDays.Value)
                    .ToList();
            }

            rows = string.Equals(
                order,
                "asc",
                StringComparison.OrdinalIgnoreCase)
                ? rows
                    .OrderBy(x => x.days_in_inventory)
                    .ThenBy(x => x.product_name)
                    .ThenBy(x => x.lot_no)
                    .ToList()
                : rows
                    .OrderByDescending(x =>
                        x.needs_verification)
                    .ThenByDescending(x =>
                        x.days_in_inventory)
                    .ThenBy(x =>
                        x.product_name)
                    .ThenBy(x =>
                        x.lot_no)
                    .ToList();

            var total = rows.Count;

            var pagedRows = rows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new Dictionary<string, object>
    {
        { "data", pagedRows },
        { "total", total },
        { "page", page },
        { "pageSize", pageSize },
        {
            "totalPages",
            (int)Math.Ceiling(
                total / (double)pageSize
            )
        },

        {
            "summary",
            new
            {
                total_lots = rows.Count,

                needs_verification =
                    rows.Count(x =>
                        x.needs_verification),

                near_expiry =
                    rows.Count(x =>
                        x.aging_status ==
                        "NEAR_EXPIRY"),

                expired =
                    rows.Count(x =>
                        x.aging_status ==
                        "EXPIRED"),

                cleanup_needed =
                    rows.Count(x =>
                        x.aging_status ==
                        "CLEANUP_NEEDED"),

                transaction_issue =
                    rows.Count(x =>
                        x.aging_status ==
                        "TRANSACTION_ISSUE"),

                        verified_today =
    rows.Count(x =>
        x.last_verified_at.HasValue &&
        x.last_verified_at.Value.Date == todayPh)


            }
        }
    };
        }



        public async Task<List<InventoryPrintSummaryDto>>
         GetPrintSummaryAsync(
             string search = "",
             string warehouse = "",
             string categories = "",
             string stockStatus = "",
             string order = "asc")
        {
            var productSummary =
                await GetProductSummaryAsync(
                    search,
                    warehouse,
                    categories,
                    stockStatus,
                    order
                );

            return productSummary
                .Select(x => new InventoryPrintSummaryDto
                {
                    product_id = x.ProductId,
                    product_name = x.ProductName,
                    product_description =
                        x.ProductDescription,

                    category_name = x.CategoryName,

                    available_qty = x.AvailableQty,

                    uom = x.Uom,
                    pack_qty = x.PackQty,
                    pack_uom = x.PackUom,
                    pack_display = x.PackDisplay
                })
                .ToList();
        }


        private static string FormatPackForPrint(
    decimal quantity,
    decimal packQty,
    string packUom,
    string baseUom)
        {
            if (quantity <= 0)
            {
                return packQty > 0 && !string.IsNullOrWhiteSpace(packUom)
                    ? $"0 {packUom}"
                    : $"0 {baseUom}";
            }

            if (packQty <= 0 || string.IsNullOrWhiteSpace(packUom))
            {
                return $"{FormatNumber(quantity)} {baseUom}";
            }

            decimal fullPackDecimal = Math.Floor(quantity / packQty);
            decimal remainder = quantity - (fullPackDecimal * packQty);

            var parts = new List<string>();

            if (fullPackDecimal > 0)
            {
                parts.Add(
                    $"{FormatNumber(fullPackDecimal)} {packUom}"
                );
            }

            if (remainder > 0)
            {
                parts.Add(
                    $"{FormatNumber(remainder)} {baseUom}"
                );
            }

            if (parts.Count == 0)
            {
                return $"0 {packUom}";
            }

            return string.Join(" & ", parts);
        }

        private static string FormatNumber(decimal value)
        {
            return value % 1 == 0
                ? value.ToString("0")
                : value.ToString("0.##");
        }



        public async Task<List<string>> GetInventoryCategoriesAsync()
        {
            return await (
                from lot in _context.ProductLotNumbers
                join product in _context.Products
                    on lot.product_id equals product.product_id
                join category in _context.Categories
                    on product.catg_id equals category.catg_id
                where
     !lot.is_deleted &&
     !product.is_deleted &&
     category.catg_name != null &&
     category.catg_name != ""


                select category.catg_name
            )
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        }

        private static DateTime ConvertToPhilippineTime(DateTime dateTime, TimeZoneInfo phTimeZone)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return TimeZoneInfo.ConvertTimeFromUtc(dateTime, phTimeZone);

            if (dateTime.Kind == DateTimeKind.Local)
                return TimeZoneInfo.ConvertTime(dateTime, phTimeZone);

            return dateTime;
        }

        public async Task<object> UpdateLotDatesAsync(UpdateLotDatesDto dto)
        {
            var lot = await _context.ProductLotNumbers
                .FirstOrDefaultAsync(x =>
                    !x.is_deleted &&
                    x.product_id == dto.product_id &&
                    x.branch_id == dto.branch_id &&
                    x.lot_no == dto.lot_no
                );

            if (lot == null)
                throw new Exception("Lot not found.");

            if (!dto.manufacturing_date.HasValue)
                throw new Exception("Manufacturing date is required.");

            if (!dto.expiration_date.HasValue)
                throw new Exception("Expiration date is required.");

            if (dto.expiration_date.Value.Date < dto.manufacturing_date.Value.Date)
                throw new Exception("Expiration date cannot be earlier than manufacturing date.");

            lot.manufacturing_date = dto.manufacturing_date.Value.Date;
            lot.expiration_date = dto.expiration_date.Value.Date;
            lot.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new
            {
                success = true,
                message = "Lot dates updated successfully."
            };
        }

        public async Task<List<InventoryProductSummaryDto>> GetProductSummaryAsync(
     string search = "",
     string warehouse = "",
     string categories = "",
     string stockStatus = "",
     string order = "asc")
        {
            var productQuery =
     from product in _context.Products
     where !product.is_deleted

     join categoryData in _context.Categories
         on product.catg_id equals categoryData.catg_id
         into categoryJoin

     from categoryData in categoryJoin.DefaultIfEmpty()

     select new
     {
         product_id = product.product_id,
         product_name = product.product_name ?? "",
         product_description = product.product_description ?? "",
         category_name = categoryData != null
             ? categoryData.catg_name ?? ""
             : "Uncategorized",
         uom = product.uom ?? "",
         pack_qty = (decimal)(product.pack_qty ?? 0),
         pack_uom = product.pack_uom ?? "",
         stock_level = product.stock_level
     };

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();

                productQuery = productQuery.Where(x =>
                    x.product_name.Contains(keyword) ||
                    x.product_description.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(categories))
            {
                var selectedCategories = categories
                    .Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries
                    )
                    .ToList();

                if (selectedCategories.Count > 0)
                {
                    productQuery = productQuery.Where(x =>
                        selectedCategories.Contains(x.category_name));
                }
            }

            var products = await productQuery.ToListAsync();

            var lotQuery = _context.ProductLotNumbers
                .Where(x => !x.is_deleted);

            if (!string.IsNullOrWhiteSpace(warehouse))
            {
                lotQuery = lotQuery.Where(x =>
                    x.branch_id == warehouse);
            }

            var lots = await lotQuery
                .Select(x => new
                {
                    product_id = x.product_id,
                    branch_id = x.branch_id,
                    lot_no = x.lot_no ?? "",
                    quantity = (decimal)x.quantity
                })
                .ToListAsync();

            var allocationQuery =
                from allocation in _context.DailyOrderAllocations

                join line in _context.DailyOrderLines
                    on allocation.order_line_id equals line.order_line_id

                join header in _context.DailyOrderHeaders
                    on line.order_id equals header.order_id

                where !header.is_deleted
                      && allocation.allocated_qty > 0

                select new
                {
                    product_id = allocation.product_id,
                    branch_id = allocation.branch_id,
                    lot_no = allocation.lot_no ?? "",
                    reserved_qty = allocation.allocated_qty
                };

            if (!string.IsNullOrWhiteSpace(warehouse))
            {
                allocationQuery = allocationQuery.Where(x =>
                    x.branch_id == warehouse);
            }

            var allocations =
                await allocationQuery.ToListAsync();

            var result = products.Select(product =>
            {
                var productLots = lots
                    .Where(x =>
                        (x.product_id ?? "").Trim() ==
                        (product.product_id ?? "").Trim())
                    .ToList();

                decimal totalQty =
                    productLots.Sum(x => x.quantity);

                decimal reservedQty = 0;

                foreach (var lot in productLots)
                {
                    reservedQty += allocations
                        .Where(a =>
                            (a.product_id ?? "").Trim() ==
                            (lot.product_id ?? "").Trim() &&

                            (a.branch_id ?? "").Trim() ==
                            (lot.branch_id ?? "").Trim() &&

                            (a.lot_no ?? "").Trim() ==
                            (lot.lot_no ?? "").Trim())
                        .Sum(a => a.reserved_qty);
                }

                decimal availableQty =
                    Math.Max(0, totalQty - reservedQty);

                string stockStatusValue;

                if (totalQty <= 0)
                {
                    stockStatusValue = "OUT OF STOCK";
                }
                else if (
                    product.stock_level > 0 &&
                    totalQty <= product.stock_level)
                {
                    stockStatusValue = "LOW STOCK";
                }
                else
                {
                    stockStatusValue = "NORMAL";
                }

                decimal deficitQty =
                    Math.Max(
                        product.stock_level - totalQty,
                        0
                    );

                return new InventoryProductSummaryDto
                {
                    ProductId = product.product_id,
                    ProductName = product.product_name,
                    ProductDescription =
                        product.product_description,

                    CategoryName = product.category_name,

                    TotalQty = totalQty,
                    ReservedQty = reservedQty,
                    AvailableQty = availableQty,

                    StockLevel = product.stock_level,
                    DeficitQty = deficitQty,

                    Uom = product.uom,
                    PackQty = product.pack_qty,
                    PackUom = product.pack_uom,

                    PackDisplay = FormatPackForPrint(
                        availableQty,
                        product.pack_qty,
                        product.pack_uom,
                        product.uom
                    ),

                    StockStatus = stockStatusValue
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                var normalizedStatus =
                    stockStatus.Trim().ToLower();

                if (normalizedStatus == "zero")
                {
                    result = result
                        .Where(x =>
                            x.StockStatus == "OUT OF STOCK")
                        .ToList();
                }
                else if (normalizedStatus == "low")
                {
                    result = result
                        .Where(x =>
                            x.StockStatus == "LOW STOCK")
                        .ToList();
                }
                else if (normalizedStatus == "low-or-zero")
                {
                    result = result
                        .Where(x =>
                            x.StockStatus == "LOW STOCK" ||
                            x.StockStatus == "OUT OF STOCK")
                        .ToList();
                }
                else if (normalizedStatus == "normal")
                {
                    result = result
                        .Where(x =>
                            x.StockStatus == "NORMAL")
                        .ToList();
                }
            }

            result = string.Equals(
                order,
                "desc",
                StringComparison.OrdinalIgnoreCase)
                ? result
                    .OrderByDescending(x => x.CategoryName)
                    .ThenByDescending(x => x.ProductName)
                    .ToList()
                : result
                    .OrderBy(x => x.CategoryName)
                    .ThenBy(x => x.ProductName)
                    .ToList();


            return result;
        }


        public async Task<Dictionary<string, object>>
            GetProductSummaryPagedAsync(
                int page = 1,
                int pageSize = 25,
                string search = "",
                string warehouse = "",
                string categories = "",
                string stockStatus = "",
                string order = "asc")
        {
            var result = await GetProductSummaryAsync(
                search,
                warehouse,
                categories,
                stockStatus,
                order);

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 10, 100);

            var total = result.Count;

            var pagedData = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new Dictionary<string, object>
    {
        { "data", pagedData },
        { "total", total },
        { "page", page },
        { "pageSize", pageSize },
        { "totalPages", (int)Math.Ceiling(total / (double)pageSize) }
    };
        }



    }
}