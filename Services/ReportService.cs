using inventory_api.Data;
using inventory_api.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        private static int? GetMonthsLeft(DateTime? expiryDate)
        {
            if (expiryDate == null) return null;

            var today = DateTime.UtcNow.Date;
            var exp = expiryDate.Value.Date;

            var months = ((exp.Year - today.Year) * 12) + exp.Month - today.Month;

            if (exp.Day < today.Day)
                months--;

            return months;
        }

        private static string GetExpiryStatus(DateTime? expiryDate)
        {
            var monthsLeft = GetMonthsLeft(expiryDate);

            if (monthsLeft == null)
                return "NO EXPIRY";

            if (monthsLeft < 0)
                return "EXPIRED";

            if (monthsLeft <= 3)
                return "NEAR";

            return "SAFE";
        }

        public async Task<DeliveryKpiReportDto> GetDeliveryKpiAsync(ReportFilterDto filter)
        {
            var data = await (
                from order in _context.DailyOrderHeaders.AsNoTracking()
                join customer in _context.Partners.AsNoTracking()
                    on order.customer_id equals customer.partner_id into customerJoin
                from customer in customerJoin.DefaultIfEmpty()
                where order.date_delivered != null
                select new
                {
                    order.order_id,
                    order.order_no,
                    order.customer_id,
                    CustomerName = customer != null ? customer.partner_name : "",
                    CustomerRegion = customer != null ? customer.region : "",
                    order.route_name,
                    order.date_ordered,
                    order.date_delivered
                }
            ).ToListAsync();




            if (filter.DateFrom.HasValue)
                data = data.Where(x => x.date_ordered >= filter.DateFrom.Value).ToList();

            if (filter.DateTo.HasValue)
            {
                var toDate = filter.DateTo.Value.Date.AddDays(1);
                data = data.Where(x => x.date_ordered < toDate).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerId))
                data = data.Where(x => x.customer_id == filter.CustomerId).ToList();

            if (!string.IsNullOrWhiteSpace(filter.RouteName))
                data = data.Where(x => x.route_name == filter.RouteName).ToList();


            var orderIds = data.Select(x => x.order_id).Distinct().ToList();

            var transactions = await _context.InventoryTransactions
                .AsNoTracking()
                .Where(t => t.order_id != null && orderIds.Contains(t.order_id.Value))
                .Select(t => new
                {
                    OrderId = t.order_id!.Value,
                    t.dr_no,
                    t.inv_no,
                    t.po_no,
                    t.checklist_no,
                    t.order_no,
                    t.remarks
                })
                .ToListAsync();

            var transactionRefs = new Dictionary<long, string>();

            foreach (var group in transactions.GroupBy(x => x.OrderId))
            {
                var refs = group
                    .Select(t =>
                        !string.IsNullOrWhiteSpace(t.dr_no) ? "DR: " + t.dr_no :
                        !string.IsNullOrWhiteSpace(t.inv_no) ? "INV: " + t.inv_no :
                        !string.IsNullOrWhiteSpace(t.po_no) ? "PO: " + t.po_no :
                        !string.IsNullOrWhiteSpace(t.checklist_no) ? "DC: " + t.checklist_no :
                        !string.IsNullOrWhiteSpace(t.order_no) ? "DO: " + t.order_no :
                        !string.IsNullOrWhiteSpace(t.remarks) ? "Remarks: " + t.remarks :
                        ""
                    )
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                transactionRefs[group.Key] = string.Join(" | ", refs);
            }



            var rows = data.Select(x =>
            {
                if (x.date_ordered == null || x.date_delivered == null)
                    return null;

                var orderedDate = x.date_ordered.Value.Date;
                var deliveredDate = x.date_delivered.Value.Date;

                var days = Math.Max(0, (deliveredDate - orderedDate).Days);

                var region = (x.CustomerRegion ?? "").ToUpper();

                var targetDays = region == "LUZON" ? 3 :
                                 region == "VIZMIN" ? 5 :
                                 0;

                return new DeliveryKpiRowDto
                {
                    OrderNo = x.order_no,
                    CustomerName = x.CustomerName,
                    RouteName = x.route_name ?? "",
                    Region = region,
                    DateOrdered = x.date_ordered,
                    DateDelivered = x.date_delivered,
                    DeliveryDays = days,
                    TargetDays = targetDays,
                    KpiStatus = targetDays == 0
        ? "NO REGION"
        : days <= targetDays ? "ON TIME" : "DELAYED",
                    TransactionReference = transactionRefs.ContainsKey(x.order_id)
        ? transactionRefs[x.order_id]
        : ""
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(filter.Region))
                rows = rows.Where(x => x.Region == filter.Region.ToUpper()).ToList();

            var total = rows.Count;
            var onTime = rows.Count(x => x.KpiStatus == "ON TIME");
            var delayed = rows.Count(x => x.KpiStatus == "DELAYED");

            return new DeliveryKpiReportDto
            {
                Summary = new DeliveryKpiSummaryDto
                {
                    TotalDeliveries = total,
                    OnTimePercent = total == 0 ? 0 : Math.Round((decimal)onTime / total * 100, 2),
                    DelayedPercent = total == 0 ? 0 : Math.Round((decimal)delayed / total * 100, 2),
                    AverageDeliveryDays = total == 0
    ? 0
    : Math.Round(Convert.ToDecimal(rows.Average(x => x.DeliveryDays)), 2)
                },
                Items = rows
                    .OrderByDescending(x => x.DateDelivered)
                    .ToList()
            };
        }

        public async Task<NearExpiryReportDto> GetNearExpiryAsync(ReportFilterDto filter)
        {
            var data = await (
                from lot in _context.ProductLotNumbers.AsNoTracking()
                join product in _context.Products.AsNoTracking()
                    on lot.product_id equals product.product_id
                join branch in _context.Branches.AsNoTracking()
                    on lot.branch_id equals branch.branch_id
                where !lot.is_deleted
                select new NearExpiryRowDto
                {
                    ProductId = lot.product_id,
                    ProductName = product.product_name,
                    LotNo = lot.lot_no,
                    BranchId = lot.branch_id,
                    BranchName = branch.branch_name,
                    Quantity = lot.quantity,
                    Uom = product.uom,
                    PackQty = product.pack_qty,
                    PackUom = product.pack_uom,
                    ManufacturingDate = lot.manufacturing_date,
                    ExpirationDate = lot.expiration_date
                }
            ).ToListAsync();

            if (!string.IsNullOrWhiteSpace(filter.BranchId))
                data = data.Where(x => x.BranchId == filter.BranchId).ToList();

            if (!string.IsNullOrWhiteSpace(filter.ProductId))
                data = data.Where(x => x.ProductId == filter.ProductId).ToList();

            foreach (var item in data)
            {
                item.MonthsLeft = GetMonthsLeft(item.ExpirationDate);
                item.ExpiryStatus = GetExpiryStatus(item.ExpirationDate);
            }

            data = data
                .Where(x => x.ExpiryStatus == "EXPIRED" || x.ExpiryStatus == "NEAR")
                .ToList();

            if (!string.IsNullOrWhiteSpace(filter.ExpiryStatus))
                data = data.Where(x => x.ExpiryStatus == filter.ExpiryStatus.ToUpper()).ToList();

            if (filter.MonthsLeft.HasValue)
                data = data.Where(x => x.MonthsLeft <= filter.MonthsLeft.Value).ToList();

            return new NearExpiryReportDto
            {
                TotalLots = data.Count,
                ExpiredLots = data.Count(x => x.ExpiryStatus == "EXPIRED"),
                NearExpiryLots = data.Count(x => x.ExpiryStatus == "NEAR"),
                Items = data
                    .OrderBy(x => x.ExpirationDate)
                    .ThenBy(x => x.ProductName)
                    .ToList()
            };
        }

        public async Task<InventoryReportDto> GetInventoryAsync(ReportFilterDto filter)
        {
            var data = await (
                from lot in _context.ProductLotNumbers.AsNoTracking()
                join product in _context.Products.AsNoTracking()
                    on lot.product_id equals product.product_id
                join branch in _context.Branches.AsNoTracking()
                    on lot.branch_id equals branch.branch_id
                where !lot.is_deleted
                select new InventoryReportRowDto
                {
                    ProductId = lot.product_id,
                    ProductName = product.product_name,
                    BranchId = lot.branch_id,
                    BranchName = branch.branch_name,
                    LotNo = lot.lot_no,
                    Quantity = lot.quantity,
                    Uom = product.uom,
                    PackQty = product.pack_qty,
                    PackUom = product.pack_uom,
                    ExpirationDate = lot.expiration_date
                }
            ).ToListAsync();

            if (!string.IsNullOrWhiteSpace(filter.BranchId))
                data = data.Where(x => x.BranchId == filter.BranchId).ToList();

            if (!string.IsNullOrWhiteSpace(filter.ProductId))
                data = data.Where(x => x.ProductId == filter.ProductId).ToList();

            foreach (var item in data)
            {
                item.MonthsLeft = GetMonthsLeft(item.ExpirationDate);
                item.ExpiryStatus = GetExpiryStatus(item.ExpirationDate);
                item.StockStatus = item.Quantity <= 0 ? "ZERO" : "WITH STOCK";
            }

            if (!string.IsNullOrWhiteSpace(filter.ExpiryStatus))
                data = data.Where(x => x.ExpiryStatus == filter.ExpiryStatus.ToUpper()).ToList();

            if (!string.IsNullOrWhiteSpace(filter.StockStatus))
                data = data.Where(x => x.StockStatus == filter.StockStatus.ToUpper()).ToList();

            return new InventoryReportDto
            {
                TotalStockQty = data.Sum(x => x.Quantity),
                TotalLots = data.Count,
                ExpiredLots = data.Count(x => x.ExpiryStatus == "EXPIRED"),
                NearExpiryLots = data.Count(x => x.ExpiryStatus == "NEAR"),
                Items = data
                    .OrderBy(x => x.ProductName)
                    .ThenBy(x => x.BranchName)
                    .ThenBy(x => x.ExpirationDate)
                    .ToList()
            };
        }

        public async Task<ReturnReportDto> GetReturnsAsync(ReportFilterDto filter)
        {
            var rows = await (
                from h in _context.ReturnHeaders.AsNoTracking()
                join l in _context.ReturnLines.AsNoTracking()
                    on h.return_id equals l.return_id
                where !h.is_deleted
                select new ReturnReportRowDto
                {
                    ReturnNo = h.return_no,
                    ReturnDate = h.return_date,

                    CustomerName = h.customer_name ?? "",

                    ProductName = l.product_name,
                    LotNo = l.lot_no ?? "",
                    Quantity = l.quantity,
                    Uom = l.uom ?? "",

                    Reason = h.reason ?? "",
                    Remarks = !string.IsNullOrWhiteSpace(l.remarks)
                        ? l.remarks
                        : h.remarks ?? "",

                    Status = h.status,

                    LinkedTransactionNo =
                        !string.IsNullOrWhiteSpace(l.dr_no) ? "DR: " + l.dr_no :
                        !string.IsNullOrWhiteSpace(l.inv_no) ? "INV: " + l.inv_no :
                        !string.IsNullOrWhiteSpace(l.po_no) ? "PO: " + l.po_no :
                        !string.IsNullOrWhiteSpace(l.checklist_no) ? "DC: " + l.checklist_no :
                        !string.IsNullOrWhiteSpace(l.order_no) ? "DO: " + l.order_no :
                        l.source_transaction_id != null ? "TRN ID: " + l.source_transaction_id :
                        ""
                }
            ).ToListAsync();

            if (filter.DateFrom.HasValue)
                rows = rows.Where(x => x.ReturnDate >= filter.DateFrom.Value).ToList();

            if (filter.DateTo.HasValue)
            {
                var toDate = filter.DateTo.Value.Date.AddDays(1);
                rows = rows.Where(x => x.ReturnDate < toDate).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerId))
            {
                rows = rows.Where(x =>
                    x.CustomerName.Contains(filter.CustomerId, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var topReason = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Reason))
                .GroupBy(x => x.Reason)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .Select(g => g.Key)
                .FirstOrDefault() ?? "";

            var topCustomer = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.CustomerName))
                .GroupBy(x => x.CustomerName)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .Select(g => g.Key)
                .FirstOrDefault() ?? "";

            return new ReturnReportDto
            {
                Summary = new ReturnReportSummaryDto
                {
                    TotalReturnedQty = rows.Sum(x => x.Quantity),
                    TopReason = topReason,
                    TopCustomer = topCustomer
                },
                Items = rows
                    .OrderByDescending(x => x.ReturnDate)
                    .ToList()
            };
        }
    }
}