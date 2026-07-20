using inventory_api.Data;
using inventory_api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;
        private readonly InventoryDisplayService _inventoryDisplayService;

        public DashboardService(
     AppDbContext context,
     InventoryDisplayService inventoryDisplayService)
        {
            _context = context;
            _inventoryDisplayService =
                inventoryDisplayService;
        }
        public async Task<DashboardDto> GetDashboardAsync()
        {
            var dto = new DashboardDto();

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

            var phToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            var phTomorrow = phToday.AddDays(1);

            var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(phToday, tz);
            var tomorrowStartUtc = TimeZoneInfo.ConvertTimeToUtc(phTomorrow, tz);

            // =========================
            // SUMMARY CARDS
            // =========================

            dto.DailyOrders = await _context.DailyOrderHeaders
                .Where(x =>
                    !x.is_deleted &&
                    (x.status ?? "").Trim().ToUpper() != "COMPLETED" &&
                    (x.status ?? "").Trim().ToUpper() != "CANCELLED")
                .CountAsync();

            dto.ReadyForChecklist = await _context.DailyOrderHeaders
                .Where(x =>
                    !x.is_deleted &&
                    (x.status ?? "").Trim().ToUpper() == "READY FOR DISPATCH" &&
                    !_context.DeliveryChecklistLines.Any(cl =>
                        cl.order_id == x.order_id &&
                        _context.DeliveryChecklistHeaders.Any(ch =>
                            ch.checklist_id == cl.checklist_id &&
                            !ch.is_deleted &&
                            (
                                (ch.status ?? "").Trim().ToUpper() == "READY" ||
                                (ch.status ?? "").Trim().ToUpper() == "LOADING" ||
                                (ch.status ?? "").Trim().ToUpper() == "PARTIAL"
                            )
                        )
                    )
                )
                .CountAsync();

            dto.ChecklistQueue = await _context.DeliveryChecklistHeaders
                .Where(x =>
                    !x.is_deleted &&
                    x.created_at >= todayStartUtc &&
                    x.created_at < tomorrowStartUtc &&
                    (
                        (x.status ?? "").Trim().ToUpper() == "READY" ||
                        (x.status ?? "").Trim().ToUpper() == "LOADING" ||
                        (x.status ?? "").Trim().ToUpper() == "PARTIAL"
                    ))
                .CountAsync();

            dto.ReleasedToday = await _context.InventoryTransactions
                .Where(x =>
                    x.created_at >= todayStartUtc &&
                    x.created_at < tomorrowStartUtc &&
                    (x.reference_type ?? "").Trim().ToUpper().Contains("CHECKLIST"))
                .CountAsync();

            dto.PartialDispatch = await _context.DailyOrderHeaders
                .Where(x =>
                    !x.is_deleted &&
                    (
                        (x.status ?? "").Trim().ToUpper() == "PARTIALLY DELIVERED" ||
                        (x.status ?? "").Trim().ToUpper() == "PARTIALLY DISPATCHED"
                    ))
                .CountAsync();

            var alerts = new List<DashboardInventoryAlertDto>();

            // =========================
            // LOW STOCK / OUT OF STOCK
            // =========================

            var allProductInventorySummary =
       await _inventoryDisplayService
           .GetProductSummaryAsync();

            dto.LowStockProducts =
    allProductInventorySummary
        .Where(x =>
            x.StockLevel > 0 &&
            (
                x.StockStatus == "LOW STOCK" ||
                x.StockStatus == "OUT OF STOCK"
            ))
        .OrderBy(x => x.CategoryName)
        .ThenBy(x =>
            x.StockStatus == "OUT OF STOCK" ? 0 : 1)
        .ThenByDescending(x => x.DeficitQty)
        .ThenBy(x => x.ProductName)
        .ToList();

            dto.LowStock = dto.LowStockProducts.Count;

            var stockDict = allProductInventorySummary
                .ToDictionary(
                    x => x.ProductId,
                    x => x.TotalQty
                );

            var reservedDict = allProductInventorySummary
                .ToDictionary(
                    x => x.ProductId,
                    x => x.ReservedQty
                );

            foreach (var item in dto.LowStockProducts)
            {
                alerts.Add(new DashboardInventoryAlertDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,

                    Quantity = item.TotalQty,
                    StockLevel = item.StockLevel,

                    AvailableQty = item.AvailableQty,
                    ReservedQty = item.ReservedQty,

                    Uom = item.Uom,
                    AlertType = item.StockStatus
                });
            }

            // =========================
            // PLANNING SHORTAGE
            // =========================

            var shortageRaw = await _context.DailyOrderHeaders
                .Where(h =>
                    !h.is_deleted &&
                    (h.status ?? "").ToUpper() != "COMPLETED" &&
                    (h.status ?? "").ToUpper() != "CANCELLED")
                .SelectMany(h => h.Lines)
                .Where(l => l.required_qty > l.allocated_qty)
                .Join(_context.Products,
                    l => l.product_id,
                    p => p.product_id,
                    (l, p) => new
                    {
                        l.product_id,
                        ProductName = p.product_name,
                        p.uom,
                        l.required_qty,
                        l.allocated_qty
                    })
                .GroupBy(x => new
                {
                    x.product_id,
                    x.ProductName,
                    x.uom
                })
                .Select(g => new
                {
                    ProductId = g.Key.product_id,
                    ProductName = g.Key.ProductName,
                    Uom = g.Key.uom,
                    Required = g.Sum(x => x.required_qty),
                    Allocated = g.Sum(x => x.allocated_qty),
                    Shortage = g.Sum(x => x.required_qty - x.allocated_qty)
                })
                .ToListAsync();

            foreach (var s in shortageRaw)
            {
                var onHand = stockDict.ContainsKey(s.ProductId)
                    ? stockDict[s.ProductId]
                    : 0;

                var reserved = reservedDict.ContainsKey(s.ProductId)
                    ? reservedDict[s.ProductId]
                    : 0;

                // actual available stock after reservation
                var available = onHand - reserved;

                // qty still needed for this planning
                var unallocatedNeed = s.Required - s.Allocated;

                // REAL shortage only if available cannot cover remaining need
                var shortageQty = Math.Max(0, unallocatedNeed - available);

                // only show shortage if truly insufficient
                if (shortageQty <= 0)
                    continue;

                alerts.Add(new DashboardInventoryAlertDto
                {
                    ProductId = s.ProductId,
                    ProductName = s.ProductName,
                    StockLevel = 0,

                    AvailableQty = available,
                    ReservedQty = reserved,
                    RequiredQty = s.Required,
                    ShortageQty = shortageQty,

                    Quantity = shortageQty,
                    Uom = s.Uom ?? "",
                    AlertType = "PLANNING SHORTAGE"
                });
            }

            // =========================
            // NEAR EXPIRY / EXPIRED ALERTS
            // Near Expiry = within 3 months
            // =========================

            var nearExpiryLimit = phToday.AddMonths(3);

            var expiryAlerts = await _context.ProductLotNumbers
      .Where(l =>
          !l.is_deleted &&
          l.quantity > 0 && // only show if there is stock
          l.expiration_date.HasValue)
      .Join(_context.Products,
          l => l.product_id,
          p => p.product_id,
          (l, p) => new
          {
              Lot = l,
              Product = p
          })
      .ToListAsync();

            foreach (var x in expiryAlerts)
            {
                var expDate = x.Lot.expiration_date!.Value.Date;

                int monthsLeft =
                    ((expDate.Year - phToday.Year) * 12) +
                    expDate.Month - phToday.Month;

                if (expDate.Day < phToday.Day)
                    monthsLeft--;

                // only include expired or 3 months below
                if (monthsLeft > 3)
                    continue;

                alerts.Add(new DashboardInventoryAlertDto
                {
                    ProductId = x.Product.product_id,
                    ProductName = x.Product.product_name,
                    LotNo = x.Lot.lot_no,
                    Quantity = x.Lot.quantity,
                    Uom = x.Product.uom ?? "",
                    ExpirationDate = x.Lot.expiration_date,
                    MonthsLeft = monthsLeft,
                    AlertType = monthsLeft < 0 ? "EXPIRED" : "EXPIRING SOON"
                });
            }

            // =========================
            // FINAL ALERT OUTPUT
            // =========================

            dto.InventoryAlerts = alerts
                .GroupBy(x => new
                {
                    x.ProductId,
                    LotNo = x.LotNo ?? "",
                    x.AlertType
                })
                .Select(g => g.First())
                .OrderBy(x =>
                    x.AlertType == "EXPIRED" ? 0 :
                    x.AlertType == "OUT OF STOCK" ? 1 :
                    x.AlertType == "PLANNING SHORTAGE" ? 2 :
                    x.AlertType == "EXPIRING SOON" ? 3 :
                    x.AlertType == "LOW STOCK" ? 4 : 5)
                .ThenBy(x => x.ProductName)
                .Take(5)
                .ToList();

            dto.LowStock = dto.LowStockProducts.Count;

            // =========================
            // COMPLETED TODAY
            // =========================

            dto.CompletedOrders = await _context.DailyOrderHeaders
                .Where(x =>
                    !x.is_deleted &&
                    (x.status ?? "").Trim().ToUpper() == "COMPLETED" &&
                    x.date_delivered.HasValue &&
                    x.date_delivered.Value >= todayStartUtc &&
                    x.date_delivered.Value < tomorrowStartUtc)
                .CountAsync();

            // =========================
            // DELIVERY CHECKLIST LIST
            // =========================

            dto.Checklist = await _context.DeliveryChecklistHeaders
                .Where(x =>
                    !x.is_deleted &&
                    x.created_at >= todayStartUtc &&
                    x.created_at < tomorrowStartUtc &&
                    (
                        (x.status ?? "").Trim().ToUpper() == "READY" ||
                        (x.status ?? "").Trim().ToUpper() == "LOADING" ||
                        (x.status ?? "").Trim().ToUpper() == "PARTIAL" ||
                        (x.status ?? "").Trim().ToUpper() == "COMPLETED"
                    ))
                .OrderByDescending(x => x.checklist_id)
                .Take(5)
                .Select(x => new DashboardChecklistDto
                {
                    ChecklistId = x.checklist_id,
                    ChecklistNo = x.checklist_no,
                    DeliveryDate = x.delivery_date,
                    TruckName = x.truck_name,
                    DriverName = x.driver_name,
                    Status = x.status
                })
                .ToListAsync();

            // =========================
            // PARTIAL ORDERS LIST
            // =========================

            dto.PartialOrders = await _context.DailyOrderHeaders
                .Where(x =>
                    !x.is_deleted &&
                    (
                        (x.status ?? "").Trim().ToUpper() == "PARTIALLY DELIVERED" ||
                        (x.status ?? "").Trim().ToUpper() == "PARTIALLY DISPATCHED"
                    ))
                .OrderByDescending(x => x.order_id)
                .Take(5)
                .Select(x => new DashboardPartialOrderDto
                {
                    OrderId = x.order_id,
                    OrderNo = x.order_no,
                    CustomerName = x.customer_name,
                    RemainingQty = x.Lines.Sum(l => l.remaining_qty),
                    Status = x.status
                })
                .ToListAsync();

            // =========================
            // RECENT INVENTORY TRANSACTIONS
            // =========================

            dto.RecentTransactions = await _context.InventoryTransactions
                .Join(_context.Products,
                    t => t.product_id,
                    p => p.product_id,
                    (t, p) => new { t, p })
                .GroupJoin(_context.Partners,
                    tp => tp.t.customer_id,
                    c => c.partner_id,
                    (tp, customers) => new { tp.t, tp.p, customer = customers.FirstOrDefault() })
                .OrderByDescending(x => x.t.created_at)
                .Take(5)
                .Select(x => new DashboardTransactionDto
                {
                    TransactionDate = x.t.created_at,
                    ReferenceNo = x.t.reference_type,
                    CustomerName = x.customer != null ? x.customer.partner_name : "",

                    DrNo = x.t.dr_no ?? "",
                    InvNo = x.t.inv_no ?? "",
                    PoNo = x.t.po_no ?? "",
                    TrNo = x.t.tr_no ?? "",
                    OrderNo = x.t.order_no ?? "",
                    ChecklistNo = x.t.checklist_no ?? "",
                    LotNo = x.t.lot_no ?? "",

                    ProductName = x.p.product_name,
                    ProductDescription = x.p.product_description,
                    Quantity = x.t.quantity,
                    Uom = x.p.uom,
                    Type = x.t.transaction_type,
                    Remarks = x.t.remarks
                })
                .ToListAsync();

            // =========================
            // RECENT RETURNS
            // =========================

            dto.RecentReturns = await _context.ReturnHeaders
                .Include(x => x.Lines)
                .Where(x => !x.is_deleted)
                .OrderByDescending(x => x.created_at)
                .Take(5)
                .Select(x => new DashboardReturnDto
                {
                    ReturnId = x.return_id,
                    ReturnNo = x.return_no,
                    CustomerName = x.customer_name ?? "",
                    Quantity = x.Lines.Sum(l => l.quantity),
                    Uom = x.Lines.Select(l => l.uom).FirstOrDefault() ?? "",
                    Reason = x.reason ?? "",
                    Status = x.status ?? "",
                    QuarantineLocation = x.Lines.Select(l => l.quarantine_location).FirstOrDefault(),
                    ReturnDate = x.return_date,

                    DrNo = x.Lines.Select(l => l.dr_no).FirstOrDefault(),
                    InvNo = x.Lines.Select(l => l.inv_no).FirstOrDefault(),
                    PoNo = x.Lines.Select(l => l.po_no).FirstOrDefault(),
                    
                    OrderNo = x.Lines.Select(l => l.order_no).FirstOrDefault(),
                    ChecklistNo = x.Lines.Select(l => l.checklist_no).FirstOrDefault()
                })
                .ToListAsync();

            return dto;
        }
 
    }
}