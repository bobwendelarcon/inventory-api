using inventory_api.Data;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.SupplierEvaluations
{
    public class SupplierEvaluationGenerationService
    {
        private readonly AppDbContext _context;

        public SupplierEvaluationGenerationService(
            AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Generates the automatic Quality, Delivery and Cost metrics
        /// for one supplier and one month.
        /// </summary>
        public async Task<SupplierEvaluationGeneratedMetrics> GenerateAsync(
            int supplierId,
            int evaluationYear,
            int evaluationMonth)
        {
            ValidatePeriod(evaluationYear, evaluationMonth);

            var supplierExists = await _context.Suppliers
                .AsNoTracking()
                .AnyAsync(x =>
                    x.SupplierId == supplierId &&
                    x.IsDeleted == false);

            if (!supplierExists)
            {
                throw new InvalidOperationException(
                    $"Supplier with ID {supplierId} was not found.");
            }

            var periodStart = new DateTime(
                evaluationYear,
                evaluationMonth,
                1);

            var periodEndExclusive = periodStart.AddMonths(1);
            var periodEnd = periodEndExclusive.AddTicks(-1);

            var quality = await GenerateQualityMetricAsync(
                supplierId,
                periodStart,
                periodEndExclusive);

            var delivery = await GenerateDeliveryMetricAsync(
                supplierId,
                periodStart,
                periodEndExclusive);

            var cost = await GenerateCostMetricAsync(
                supplierId,
                periodStart,
                periodEndExclusive);

            return new SupplierEvaluationGeneratedMetrics
            {
                SupplierId = supplierId,
                EvaluationYear = evaluationYear,
                EvaluationMonth = evaluationMonth,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,

                Quality = quality,
                Delivery = delivery,
                Cost = cost
            };
        }

        private async Task<GeneratedQualityMetric>
            GenerateQualityMetricAsync(
                int supplierId,
                DateTime periodStart,
                DateTime periodEndExclusive)
        {
            /*
             * Quality source:
             * purchasing_qc_header
             * purchasing_qc_line
             *
             * Only inspections inside the evaluation month are included.
             */

            var qcHeaders = await _context.QcInspectionHeaders
                .AsNoTracking()
               .Where(x =>
    x.SupplierId == supplierId &&
    x.Status == "COMMITTED" &&
    x.InspectionDate >= periodStart &&
    x.InspectionDate < periodEndExclusive)
                .Select(x => new
                {
                    x.QcId,
                    x.RrId,
                    x.Status,
                    x.Decision
                })
                .ToListAsync();

            if (qcHeaders.Count == 0)
            {
                return new GeneratedQualityMetric
                {
                    ReceivingReportCount = 0,
                    QcInspectionCount = 0,
                    TotalReceivedQty = 0m,
                    TotalAcceptedQty = 0m,
                    TotalRejectedQty = 0m,
                    AcceptanceRate = 0m,
                    RejectionRate = 0m,
                    QualityScore = 0m,
                    CalculationRemarks =
                        "No QA/QC inspections were found for the period."
                };
            }

            var qcIds = qcHeaders
                .Select(x => x.QcId)
                .ToList();

            var qcLines = await _context.QcInspectionLines
                .AsNoTracking()
                .Where(x => qcIds.Contains(x.QcId))
                .Select(x => new
                {
                    x.ReceivedQty,
                    x.AcceptedQty,
                    x.RejectedQty
                })
                .ToListAsync();

            var totalReceived = qcLines.Sum(x => x.ReceivedQty);
            var totalAccepted = qcLines.Sum(x => x.AcceptedQty);
            var totalRejected = qcLines.Sum(x => x.RejectedQty);

            var acceptanceRate = totalReceived > 0m
                ? totalAccepted / totalReceived * 100m
                : 0m;

            var rejectionRate = totalReceived > 0m
                ? totalRejected / totalReceived * 100m
                : 0m;

            /*
             * Current quality formula:
             *
             * Quality Score = Accepted Qty / Received Qty × 100
             *
             * This means a supplier with 100% accepted goods receives
             * a raw Quality score of 100.
             */
            var qualityScore = acceptanceRate;

            return new GeneratedQualityMetric
            {
                ReceivingReportCount = qcHeaders
                    .Select(x => x.RrId)
                    .Distinct()
                    .Count(),

                QcInspectionCount = qcHeaders.Count,

                TotalReceivedQty = RoundQuantity(totalReceived),
                TotalAcceptedQty = RoundQuantity(totalAccepted),
                TotalRejectedQty = RoundQuantity(totalRejected),

                AcceptanceRate = RoundScore(acceptanceRate),
                RejectionRate = RoundScore(rejectionRate),
                QualityScore = NormalizeScore(qualityScore),

                CalculationRemarks =
                    $"Quality was calculated from {qcHeaders.Count} " +
                    "QA/QC inspection(s)."
            };
        }

        private async Task<GeneratedDeliveryMetric>
            GenerateDeliveryMetricAsync(
                int supplierId,
                DateTime periodStart,
                DateTime periodEndExclusive)
        {
            /*
             * Delivery source:
             * purchasing_po_header
             * purchasing_po_delivery_schedule
             * purchasing_rr_header
             *
             * Schedules are selected by ScheduledDate.
             */

            var schedules = await (
                from schedule in _context.PurchaseOrderDeliverySchedules
                    .AsNoTracking()

                join po in _context.PurchaseOrderHeaders
                    .AsNoTracking()
                    on schedule.PoId equals po.PoId

                where po.SupplierId == supplierId
                      && schedule.ScheduledDate >= periodStart
                      && schedule.ScheduledDate < periodEndExclusive

                select new
                {
                    schedule.ScheduleId,
                    schedule.ScheduledDate,
                    schedule.Status
                })
                .ToListAsync();

            if (schedules.Count == 0)
            {
                return new GeneratedDeliveryMetric
                {
                    ScheduledDeliveries = 0,
                    CompletedDeliveries = 0,
                    OnTimeDeliveries = 0,
                    EarlyDeliveries = 0,
                    LateDeliveries = 0,
                    IncompleteDeliveries = 0,
                    UndeliveredSchedules = 0,
                    OnTimeDeliveryRate = 0m,
                    AverageDelayDays = 0m,
                    DeliveryScore = 0m,
                    CalculationRemarks =
                        "No delivery schedules were found for the period."
                };
            }

            var scheduleIds = schedules
                .Select(x => x.ScheduleId)
                .ToList();

            var receivingReports = await _context.ReceivingReportHeaders
     .AsNoTracking()
     .Where(x =>
         x.ScheduleId.HasValue &&
         scheduleIds.Contains(x.ScheduleId.Value))
     .Select(x => new
     {
         x.ScheduleId,
         x.DeliveryDate,
         x.Status
     })
     .ToListAsync();

            var completed = 0;
            var onTime = 0;
            var early = 0;
            var late = 0;
            var incomplete = 0;
            var undelivered = 0;

            var delayDays = new List<decimal>();

            foreach (var schedule in schedules)
            {
                var scheduleReceivingReports = receivingReports
                    .Where(x => x.ScheduleId == schedule.ScheduleId)
                    .ToList();

                if (scheduleReceivingReports.Count == 0)
                {
                    undelivered++;
                    continue;
                }

                /*
                 * Use the latest delivery date because a delivery schedule
                 * may be fulfilled through more than one receiving report.
                 */
                var actualDeliveryDate = scheduleReceivingReports
                    .Max(x => x.DeliveryDate);

                var isCompleted = string.Equals(
                    schedule.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase);

                if (isCompleted)
                {
                    completed++;
                }
                else
                {
                    incomplete++;
                }

                var difference =
                    actualDeliveryDate.Date -
                    schedule.ScheduledDate.Date;

                if (difference.TotalDays < 0)
                {
                    early++;

                    // Early deliveries are considered timely.
                    onTime++;
                }
                else if (difference.TotalDays == 0)
                {
                    onTime++;
                }
                else
                {
                    late++;
                    delayDays.Add((decimal)difference.TotalDays);
                }
            }

            /*
             * Timely means delivered on or before the scheduled date.
             *
             * Denominator uses all scheduled deliveries. Therefore,
             * undelivered schedules automatically reduce the score.
             */
            var onTimeRate = schedules.Count > 0
                ? (decimal)onTime / schedules.Count * 100m
                : 0m;

            var averageDelay = delayDays.Count > 0
                ? delayDays.Average()
                : 0m;

            var deliveryScore = onTimeRate;

            return new GeneratedDeliveryMetric
            {
                ScheduledDeliveries = schedules.Count,
                CompletedDeliveries = completed,
                OnTimeDeliveries = onTime,
                EarlyDeliveries = early,
                LateDeliveries = late,
                IncompleteDeliveries = incomplete,
                UndeliveredSchedules = undelivered,

                OnTimeDeliveryRate = RoundScore(onTimeRate),
                AverageDelayDays = RoundScore(averageDelay),
                DeliveryScore = NormalizeScore(deliveryScore),

                CalculationRemarks =
                    $"Delivery was calculated from {schedules.Count} " +
                    "scheduled delivery record(s). Early deliveries are " +
                    "included as timely deliveries."
            };
        }

        private async Task<GeneratedCostMetric>
            GenerateCostMetricAsync(
                int supplierId,
                DateTime periodStart,
                DateTime periodEndExclusive)
        {
            /*
             * Cost source:
             * purchasing_po_header
             * purchasing_po_line
             *
             * Supplier prices are compared with the average PO unit price
             * from all suppliers for the same materials and period.
             */

            var periodPoLines = await (
                from line in _context.PurchaseOrderLines
                    .AsNoTracking()

                join po in _context.PurchaseOrderHeaders
                    .AsNoTracking()
                    on line.PoId equals po.PoId

                where po.PoDate >= periodStart
                      && po.PoDate < periodEndExclusive
                      && line.PoQty > 0m
                      && line.PoUnitPrice >= 0m

                select new CostComparisonLine
                {
                    PoId = po.PoId,
                    SupplierId = po.SupplierId,
                    MaterialId = line.MaterialId,
                    Quantity = line.PoQty,
                    UnitPrice = line.PoUnitPrice,
                    LineTotal = line.LineTotal
                })
                .ToListAsync();

            var supplierLines = periodPoLines
                .Where(x => x.SupplierId == supplierId)
                .ToList();

            if (supplierLines.Count == 0)
            {
                return new GeneratedCostMetric
                {
                    PurchaseOrderCount = 0,
                    PurchaseOrderLineCount = 0,
                    TotalPurchaseAmount = 0m,
                    SupplierAverageUnitPrice = 0m,
                    ComparisonAverageUnitPrice = 0m,
                    PriceVarianceAmount = 0m,
                    PriceVariancePercentage = 0m,
                    CostScore = 0m,
                    CalculationRemarks =
                        "No purchase order lines were found for the period."
                };
            }

            var materialMarketAverages = periodPoLines
                .GroupBy(x => x.MaterialId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Average(x => x.UnitPrice));

            var comparisons = new List<MaterialPriceComparison>();

            foreach (var supplierLine in supplierLines)
            {
                if (!materialMarketAverages.TryGetValue(
                        supplierLine.MaterialId,
                        out var comparisonPrice))
                {
                    continue;
                }

                if (comparisonPrice <= 0m)
                {
                    continue;
                }

                /*
                 * 100 means equal to the comparison price.
                 * Above 100 would mean cheaper, but raw scores are capped
                 * at 100.
                 */
                if (supplierLine.UnitPrice <= 0m)
                {
                    continue;
                }

                var lineScore =
                    comparisonPrice /
                    supplierLine.UnitPrice *
                    100m;

                comparisons.Add(new MaterialPriceComparison
                {
                    SupplierPrice = supplierLine.UnitPrice,
                    ComparisonPrice = comparisonPrice,
                    Quantity = supplierLine.Quantity,
                    Score = NormalizeScore(lineScore)
                });
            }

            var totalPurchaseAmount = supplierLines.Sum(x =>
                x.LineTotal > 0m
                    ? x.LineTotal
                    : x.Quantity * x.UnitPrice);

            var totalSupplierQuantity = supplierLines.Sum(x => x.Quantity);

            var supplierAveragePrice = totalSupplierQuantity > 0m
                ? supplierLines.Sum(x => x.UnitPrice * x.Quantity) /
                  totalSupplierQuantity
                : supplierLines.Average(x => x.UnitPrice);

            var comparisonAveragePrice = comparisons.Count > 0
                ? WeightedAverage(
                    comparisons,
                    x => x.ComparisonPrice,
                    x => x.Quantity)
                : supplierAveragePrice;

            var costScore = comparisons.Count > 0
                ? WeightedAverage(
                    comparisons,
                    x => x.Score,
                    x => x.Quantity)
                : 100m;

            var varianceAmount =
                supplierAveragePrice -
                comparisonAveragePrice;

            var variancePercentage = comparisonAveragePrice > 0m
                ? varianceAmount / comparisonAveragePrice * 100m
                : 0m;

            return new GeneratedCostMetric
            {
                PurchaseOrderCount = supplierLines
                    .Select(x => x.PoId)
                    .Distinct()
                    .Count(),

                PurchaseOrderLineCount = supplierLines.Count,

                TotalPurchaseAmount =
                    RoundMoney(totalPurchaseAmount),

                SupplierAverageUnitPrice =
                    RoundMoney(supplierAveragePrice),

                ComparisonAverageUnitPrice =
                    RoundMoney(comparisonAveragePrice),

                PriceVarianceAmount =
                    RoundMoney(varianceAmount),

                PriceVariancePercentage =
                    RoundScore(variancePercentage),

                CostScore =
                    NormalizeScore(costScore),

                CalculationRemarks =
                    $"Cost competitiveness was calculated from " +
                    $"{supplierLines.Count} purchase order line(s)."
            };
        }

        private static decimal WeightedAverage<T>(
            IEnumerable<T> items,
            Func<T, decimal> valueSelector,
            Func<T, decimal> weightSelector)
        {
            var itemList = items.ToList();

            if (itemList.Count == 0)
            {
                return 0m;
            }

            var totalWeight = itemList.Sum(weightSelector);

            if (totalWeight <= 0m)
            {
                return itemList.Average(valueSelector);
            }

            var weightedTotal = itemList.Sum(x =>
                valueSelector(x) * weightSelector(x));

            return weightedTotal / totalWeight;
        }

        private static void ValidatePeriod(
            int evaluationYear,
            int evaluationMonth)
        {
            if (evaluationYear < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationYear),
                    "Evaluation year must be 2000 or later.");
            }

            if (evaluationMonth < 1 || evaluationMonth > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationMonth),
                    "Evaluation month must be between 1 and 12.");
            }
        }

        private static decimal NormalizeScore(decimal value)
        {
            value = Math.Clamp(value, 0m, 100m);

            return RoundScore(value);
        }

        private static decimal RoundScore(decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static decimal RoundQuantity(decimal value)
        {
            return Math.Round(
                value,
                4,
                MidpointRounding.AwayFromZero);
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(
                value,
                4,
                MidpointRounding.AwayFromZero);
        }

        private sealed class CostComparisonLine
        {
            public int PoId { get; set; }

            public int SupplierId { get; set; }

            public int MaterialId { get; set; }

            public decimal Quantity { get; set; }

            public decimal UnitPrice { get; set; }

            public decimal LineTotal { get; set; }
        }

        private sealed class MaterialPriceComparison
        {
            public decimal SupplierPrice { get; set; }

            public decimal ComparisonPrice { get; set; }

            public decimal Quantity { get; set; }

            public decimal Score { get; set; }
        }
    }
}