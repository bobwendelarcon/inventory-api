using inventory_api.Data;
using inventory_api.Models.Purchasing.PurchaseOrders;
using inventory_api.Models.Purchasing.QcInspections;
using inventory_api.Models.Purchasing.ReceivingReports;
using inventory_api.Models.SupplierEvaluation;
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
        /// Creates a supplier performance evaluation for one committed
        /// Receiving Report / QC inspection.
        ///
        /// This method uses the existing DbContext transaction.
        /// It does NOT start or commit its own transaction.
        /// </summary>
        public async Task<SupplierPerformanceEvaluation>
            CreateFromCommittedQcAsync(
                QcInspectionHeader qc,
                ReceivingReportHeader rr,
                PurchaseOrderHeader po,
                string generatedBy,
                DateTime now)
        {
            ArgumentNullException.ThrowIfNull(qc);
            ArgumentNullException.ThrowIfNull(rr);
            ArgumentNullException.ThrowIfNull(po);

            if (string.IsNullOrWhiteSpace(generatedBy))
            {
                throw new ArgumentException(
                    "GeneratedBy is required.",
                    nameof(generatedBy));
            }

            /*
             * Duplicate protection.
             *
             * Database also has UNIQUE(rr_id) and UNIQUE(qc_id),
             * but we check first so we return a clearer error.
             */
            var alreadyExistsLocally =
                _context.SupplierPerformanceEvaluations.Local.Any(x =>
                    x.QcId == qc.QcId ||
                    x.RrId == rr.RrId);

            var alreadyExistsInDatabase =
                await _context.SupplierPerformanceEvaluations
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.QcId == qc.QcId ||
                        x.RrId == rr.RrId);

            if (alreadyExistsLocally ||
                alreadyExistsInDatabase)
            {
                throw new InvalidOperationException(
                    $"Supplier evaluation already exists for " +
                    $"QC {qc.QcNo} / RR {rr.RrNo}.");
            }

            if (qc.Lines == null || qc.Lines.Count == 0)
            {
                throw new InvalidOperationException(
                    $"QC {qc.QcNo} does not contain any inspection lines.");
            }

            /*
             * Schedule is optional because RR.ScheduleId is nullable.
             */
            PurchaseOrderDeliverySchedule? schedule = null;

            if (rr.ScheduleId.HasValue)
            {
                schedule = await _context
                    .PurchaseOrderDeliverySchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ScheduleId == rr.ScheduleId.Value);
            }

            var evaluationNo =
                await GenerateEvaluationNumberAsync(now);

            var evaluation =
                new SupplierPerformanceEvaluation
                {
                    EvaluationNo = evaluationNo,

                    SupplierId = qc.SupplierId,

                    PoId = po.PoId,
                    ScheduleId = rr.ScheduleId,
                    RrId = rr.RrId,
                    QcId = qc.QcId,

                    EvaluationDate = now,
                    DeliveryDate = rr.DeliveryDate,

                    /*
                     * Keep legacy fields null.
                     * New evaluations are no longer monthly records.
                     */
                    EvaluationYear = null,
                    EvaluationMonth = null,
                    PeriodStart = null,
                    PeriodEnd = null,

                    QualityScore = 0m,
                    QualityWeightedScore = 0m,

                    OnTimeDeliveryScore = 0m,
                    DeliveryWeightedScore = 0m,

                    CostCompetitivenessScore = 0m,
                    CostWeightedScore = 0m,

                    ReliabilityScore = 0m,
                    ReliabilityWeightedScore = 0m,

                    TotalScore = 0m,
                    PerformanceRating = null,

                    Status = "PENDING_PURCHASING",

                    GeneratedBy = generatedBy,
                    GeneratedAt = now,

                    CreatedBy = generatedBy,
                    CreatedAt = now,

                    UpdatedBy = generatedBy,
                    UpdatedAt = now
                };

            /*
             * Build one evaluation line for every QC line/material.
             */
            foreach (var qcLine in qc.Lines)
            {
                var rrLine = rr.Lines.FirstOrDefault(x =>
                    x.RrLineId == qcLine.RrLineId);

                if (rrLine == null)
                {
                    throw new InvalidOperationException(
                        $"RR line {qcLine.RrLineId} was not found.");
                }

                var poLine = po.Lines.FirstOrDefault(x =>
                    x.PoLineId == qcLine.PoLineId);

                if (poLine == null)
                {
                    throw new InvalidOperationException(
                        $"PO line {qcLine.PoLineId} was not found.");
                }

                PurchaseOrderDeliveryScheduleLine? scheduleLine = null;

                if (rr.ScheduleId.HasValue)
                {
                    scheduleLine = await _context
                        .PurchaseOrderDeliveryScheduleLines
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.ScheduleId == rr.ScheduleId.Value &&
                            x.PoLineId == qcLine.PoLineId);
                }

                var previousUnitPrice =
                    await GetPreviousUnitPriceAsync(
                        currentPoId: po.PoId,
                        supplierId: po.SupplierId,
                        materialId: qcLine.MaterialId,
                        currentPoDate: po.PoDate);

                var line =
                    new SupplierPerformanceEvaluationLine
                    {
                        QcLineId = qcLine.QcLineId,
                        RrLineId = qcLine.RrLineId,
                        PoLineId = qcLine.PoLineId,

                        ScheduleLineId =
                            scheduleLine?.ScheduleLineId,

                        MaterialId = qcLine.MaterialId,

                        ApprovedQty = qcLine.AcceptedQty,
                        RejectedQty = qcLine.RejectedQty,
                        TotalInspectedQty = qcLine.ReceivedQty,

                        ScheduledDate =
                            schedule?.ScheduledDate,

                        ActualDeliveryDate =
                            rr.DeliveryDate,

                        ScheduledQty =
                            scheduleLine?.ScheduledQty
                            ?? rrLine.ReceiveQty,

                        /*
                         * Delivered quantity is the physical quantity
                         * received in this RR.
                         */
                        DeliveredQty =
                            rrLine.ReceiveQty,

                        NewUnitPrice =
                            poLine.PoUnitPrice,

                        PreviousUnitPrice =
                            previousUnitPrice,

                        CoaPoints = 0m,
                        TermsPoints = 0m,
                        OtherPoints = 0m,

                        ReliabilityScore = 0m,
                        ReliabilityGrade = 0m,

                        CreatedBy = generatedBy,
                        CreatedAt = now
                    };

                CalculateQuality(line);
                CalculateDelivery(line);
                CalculateCost(line);

                /*
                 * Reliability is still pending Purchasing input.
                 */
                line.TotalGrade =
                    RoundScore(
                        line.QualityGrade +
                        line.DeliveryGrade +
                        line.CostGrade +
                        line.ReliabilityGrade);

                evaluation.Lines.Add(line);
            }

            RecalculateHeader(evaluation);

            evaluation.WorkflowHistory.Add(
                new SupplierEvaluationWorkflowHistory
                {
                    FromStatus = null,
                    ToStatus = "PENDING_PURCHASING",
                    Action = "AUTO_GENERATED_FROM_QC",
                    ActionBy = generatedBy,
                    ActionAt = now,
                    Remarks =
                        $"Automatically created from QC {qc.QcNo} " +
                        $"and RR {rr.RrNo}."
                });

            /*
             * Add only.
             * SaveChanges is handled by QcInspectionService so that
             * QC commit + inventory + supplier evaluation are atomic.
             */
            _context.SupplierPerformanceEvaluations.Add(
                evaluation);

            return evaluation;
        }


        public async Task<SupplierPerformanceEvaluation>
    CreateFromApprovedPoAsync(
        PurchaseOrderHeader po,
        string generatedBy,
        DateTime now)
        {
            ArgumentNullException.ThrowIfNull(po);

            if (string.IsNullOrWhiteSpace(generatedBy))
            {
                throw new ArgumentException(
                    "GeneratedBy is required.",
                    nameof(generatedBy));
            }

            // ---------------------------------------------------------
            // Prevent duplicate evaluation for the same PO.
            // ---------------------------------------------------------
            var existingLocal =
                _context.SupplierPerformanceEvaluations
                    .Local
                    .FirstOrDefault(x =>
                        x.PoId == po.PoId);

            if (existingLocal != null)
            {
                return existingLocal;
            }

            var existing =
                await _context.SupplierPerformanceEvaluations
                    .FirstOrDefaultAsync(x =>
                        x.PoId == po.PoId);

            if (existing != null)
            {
                return existing;
            }

            // ---------------------------------------------------------
            // Generate evaluation number.
            // ---------------------------------------------------------
            var evaluationNo =
                await GenerateEvaluationNumberAsync(now);

            var evaluation =
                new SupplierPerformanceEvaluation
                {
                    EvaluationNo = evaluationNo,

                    SupplierId = po.SupplierId,

                    PoId = po.PoId,

                    // Not available yet.
                    ScheduleId = null,
                    RrId = null,
                    QcId = null,

                    // We can use PO approval/creation date as the
                    // initial evaluation date.
                    EvaluationDate = now,

                    DeliveryDate = null,

                    // Legacy monthly fields.
                    EvaluationYear = null,
                    EvaluationMonth = null,
                    PeriodStart = null,
                    PeriodEnd = null,

                    // Scores are not calculated yet.
                    QualityScore = 0m,
                    QualityWeightedScore = 0m,

                    OnTimeDeliveryScore = 0m,
                    DeliveryWeightedScore = 0m,

                    CostCompetitivenessScore = 0m,
                    CostWeightedScore = 0m,

                    ReliabilityScore = 0m,
                    ReliabilityWeightedScore = 0m,

                    TotalScore = 0m,

                    // IMPORTANT:
                    // Do not classify unfinished evaluation as POOR.
                    PerformanceRating = "PENDING",

                    Status = "PENDING_DELIVERY",

                    GeneratedBy = generatedBy,
                    GeneratedAt = now,

                    CreatedBy = generatedBy,
                    CreatedAt = now,

                    UpdatedBy = generatedBy,
                    UpdatedAt = now
                };

            evaluation.WorkflowHistory.Add(
                new SupplierEvaluationWorkflowHistory
                {
                    FromStatus = null,
                    ToStatus = "PENDING_DELIVERY",

                    Action = "CREATED_FROM_PO",

                    ActionBy = generatedBy,
                    ActionAt = now,

                    Remarks =
                        $"Supplier evaluation created from approved PO {po.PoNo}."
                });

            _context.SupplierPerformanceEvaluations.Add(
                evaluation);

            return evaluation;
        }

        private async Task<decimal?> GetPreviousUnitPriceAsync(
      int currentPoId,
      int supplierId,
      int materialId,
      DateTime currentPoDate)
        {
            /*
             * Previous price:
             *
             * SAME SUPPLIER
             * + SAME MATERIAL
             * + valid PO status
             * + PO must be before the current PO
             *
             * Same-day POs are allowed.
             * PoId is used as the tie-breaker.
             */

            return await (
                from previousLine in _context.PurchaseOrderLines
                    .AsNoTracking()

                join previousPo in _context.PurchaseOrderHeaders
                    .AsNoTracking()
                    on previousLine.PoId equals previousPo.PoId

                where previousPo.SupplierId == supplierId
                      && previousLine.MaterialId == materialId
                      && previousPo.PoId != currentPoId

                      // Previous PO:
                      // earlier date OR same date with lower PoId
                      && (
                          previousPo.PoDate < currentPoDate
                          ||
                          (
                              previousPo.PoDate == currentPoDate
                              && previousPo.PoId < currentPoId
                          )
                      )

                      && (
                          previousPo.Status == "APPROVED"
                          || previousPo.Status == "PARTIALLY_RECEIVED"
                          || previousPo.Status == "FULLY_RECEIVED"
                      )

                orderby previousPo.PoDate descending,
                        previousPo.PoId descending

                select (decimal?)previousLine.PoUnitPrice
            )
            .FirstOrDefaultAsync();
        }

        private static void CalculateQuality(
            SupplierPerformanceEvaluationLine line)
        {
            /*
             * Quality raw score:
             *
             * Accepted / Inspected × 100
             *
             * Weight = 40%
             */
            line.QualityScore =
                line.TotalInspectedQty > 0m
                    ? NormalizeScore(
                        line.ApprovedQty /
                        line.TotalInspectedQty *
                        100m)
                    : 0m;

            line.QualityGrade =
                RoundScore(
                    line.QualityScore * 0.40m);
        }

        private static void CalculateDelivery(
            SupplierPerformanceEvaluationLine line)
        {
            /*
             * ON TIME = 50% of Delivery criterion.
             *
             * Early/on-date = 100
             * Late = 0
             */
            if (line.ScheduledDate.HasValue)
            {
                line.IsOnTime =
                    line.ActualDeliveryDate.Date <=
                    line.ScheduledDate.Value.Date;

                line.OnTimeScore =
                    line.IsOnTime
                        ? 100m
                        : 0m;
            }
            else
            {
                /*
                 * No delivery schedule.
                 * Do not award an automatic on-time score.
                 */
                line.IsOnTime = false;
                line.OnTimeScore = 0m;
            }

            /*
             * IN FULL = Delivered / Scheduled × 100.
             * Cap at 100 for over-delivery.
             */
            line.InFullScore =
                line.ScheduledQty > 0m
                    ? NormalizeScore(
                        line.DeliveredQty /
                        line.ScheduledQty *
                        100m)
                    : 0m;

            /*
             * Delivery raw score:
             *
             * 50% On Time
             * 50% In Full
             */
            line.DeliveryScore =
                RoundScore(
                    line.OnTimeScore * 0.50m +
                    line.InFullScore * 0.50m);

            /*
             * Whole delivery criterion is 30%.
             */
            line.DeliveryGrade =
                RoundScore(
                    line.DeliveryScore * 0.30m);
        }

        private static void CalculateCost(
    SupplierPerformanceEvaluationLine line)
        {
            if (!line.PreviousUnitPrice.HasValue ||
                line.PreviousUnitPrice.Value <= 0m)
            {
                line.PriceChangePercent = null;
                line.CostStatus = "NO_PREVIOUS_PRICE";

                // First purchase: no basis for penalty.
                line.CostScore = 100m;
                line.CostGrade = 20m;

                return;
            }

            var previousPrice = line.PreviousUnitPrice.Value;
            var newPrice = line.NewUnitPrice;

            var changePercent =
                ((newPrice - previousPrice) / previousPrice) * 100m;

            line.PriceChangePercent = Math.Round(
                changePercent,
                4,
                MidpointRounding.AwayFromZero);

            if (changePercent < 0m)
            {
                line.CostStatus = "PRICE_DECREASED";

                line.CostScore = 100m;
                line.CostGrade = 20m;
            }
            else if (changePercent == 0m)
            {
                line.CostStatus = "PRICE_UNCHANGED";

                line.CostScore = 100m;
                line.CostGrade = 20m;
            }
            else
            {
                line.CostStatus = "PRICE_INCREASED";

                var remainingScore =
                    Math.Max(0m, 100m - changePercent);

                line.CostScore = Math.Round(
                    remainingScore,
                    2,
                    MidpointRounding.AwayFromZero);

                line.CostGrade = Math.Round(
                    remainingScore / 100m * 20m,
                    2,
                    MidpointRounding.AwayFromZero);
            }
        }

        public static void RecalculateHeader(
            SupplierPerformanceEvaluation evaluation)
        {
            if (evaluation.Lines.Count == 0)
            {
                evaluation.QualityScore = 0m;
                evaluation.QualityWeightedScore = 0m;

                evaluation.OnTimeDeliveryScore = 0m;
                evaluation.DeliveryWeightedScore = 0m;

                evaluation.CostCompetitivenessScore = 0m;
                evaluation.CostWeightedScore = 0m;

                evaluation.ReliabilityScore = 0m;
                evaluation.ReliabilityWeightedScore = 0m;

                evaluation.TotalScore = 0m;
                evaluation.PerformanceRating = null;

                return;
            }

            evaluation.QualityScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.QualityScore));

            evaluation.QualityWeightedScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.QualityGrade));

            evaluation.OnTimeDeliveryScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.DeliveryScore));

            evaluation.DeliveryWeightedScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.DeliveryGrade));

            /*
             * Lines with NO_PREVIOUS_PRICE should not make the
             * supplier look artificially expensive.
             *
             * Only lines with an actual historical comparison
             * are included in the header Cost average.
             */
            /*
  * Cost Competitiveness
  *
  * Every evaluation line participates.
  *
  * NO_PREVIOUS_PRICE receives:
  * Raw Score = 100
  * Grade     = 20
  */
            evaluation.CostCompetitivenessScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.CostScore));

            evaluation.CostWeightedScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.CostGrade));

            evaluation.ReliabilityScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.ReliabilityScore));

            evaluation.ReliabilityWeightedScore =
                RoundScore(
                    evaluation.Lines.Average(x =>
                        x.ReliabilityGrade));

            evaluation.TotalScore =
                RoundScore(
                    evaluation.QualityWeightedScore +
                    evaluation.DeliveryWeightedScore +
                    evaluation.CostWeightedScore +
                    evaluation.ReliabilityWeightedScore);

            /*
             * Do not give the final rating yet because Purchasing
             * still needs to complete Reliability / After Sales.
             */
            if (string.Equals(
     evaluation.Status,
     "FINALIZED",
     StringComparison.OrdinalIgnoreCase))
            {
                evaluation.PerformanceRating =
                    GetPerformanceRating(
                        evaluation.TotalScore);
            }
            else
            {
                evaluation.PerformanceRating =
                    "PENDING";
            }
        }

        private async Task<string>
            GenerateEvaluationNumberAsync(
                DateTime date)
        {
            var prefix =
                $"SPE-{date:yyyy}-";

            var existingNumbers =
                await _context
                    .SupplierPerformanceEvaluations
                    .AsNoTracking()
                    .Where(x =>
                        x.EvaluationNo.StartsWith(prefix))
                    .Select(x => x.EvaluationNo)
                    .ToListAsync();

            var highestSequence = 0;

            foreach (var number in existingNumbers)
            {
                var sequencePart =
                    number.Replace(
                        prefix,
                        string.Empty);

                if (int.TryParse(
                        sequencePart,
                        out var sequence) &&
                    sequence > highestSequence)
                {
                    highestSequence = sequence;
                }
            }

            return
                $"{prefix}{highestSequence + 1:0000}";
        }

        public async Task<SupplierPerformanceEvaluation>
    UpdateFromCommittedQcAsync(
        QcInspectionHeader qc,
        ReceivingReportHeader rr,
        PurchaseOrderHeader po,
        string updatedBy,
        DateTime now)
        {
            ArgumentNullException.ThrowIfNull(qc);
            ArgumentNullException.ThrowIfNull(rr);
            ArgumentNullException.ThrowIfNull(po);

            if (string.IsNullOrWhiteSpace(updatedBy))
            {
                throw new ArgumentException(
                    "UpdatedBy is required.",
                    nameof(updatedBy));
            }


            // ============================================================
            // FIND EXISTING EVALUATION CREATED DURING PO APPROVAL
            // ============================================================

            var evaluation =
                await _context
                    .SupplierPerformanceEvaluations
                    .Include(x => x.Lines)
                    .Include(x => x.WorkflowHistory)
                    .FirstOrDefaultAsync(x =>
                        x.PoId == po.PoId);

            if (evaluation == null)
            {
                throw new InvalidOperationException(
                    $"Supplier evaluation for PO {po.PoNo} was not found.");
            }


            // ============================================================
            // PROTECT COMPLETED EVALUATION
            // ============================================================

            if (string.Equals(
                evaluation.Status,
                "FINALIZED",
                StringComparison.OrdinalIgnoreCase))
            {
                return evaluation;
            }


            // ============================================================
            // DELIVERY REFERENCES
            // ============================================================

            evaluation.SupplierId =
                po.SupplierId;

            evaluation.ScheduleId =
                rr.ScheduleId;

            evaluation.RrId =
                rr.RrId;

            evaluation.QcId =
                qc.QcId;

            evaluation.DeliveryDate =
                rr.DeliveryDate;


            // ============================================================
            // LOAD DELIVERY SCHEDULE
            // ============================================================

            PurchaseOrderDeliverySchedule? schedule =
                null;

            if (rr.ScheduleId.HasValue)
            {
                schedule =
                    await _context
                        .PurchaseOrderDeliverySchedules
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.ScheduleId ==
                            rr.ScheduleId.Value);
            }


            // ============================================================
            // REBUILD AUTOMATIC EVALUATION LINES
            // ============================================================

            evaluation.Lines.Clear();


            foreach (var qcLine in qc.Lines)
            {
                var rrLine =
                    rr.Lines.FirstOrDefault(x =>
                        x.PoLineId ==
                        qcLine.PoLineId);

                if (rrLine == null)
                {
                    throw new InvalidOperationException(
                        $"RR line for PO line " +
                        $"{qcLine.PoLineId} was not found.");
                }


                var poLine =
                    po.Lines.FirstOrDefault(x =>
                        x.PoLineId ==
                        qcLine.PoLineId);

                if (poLine == null)
                {
                    throw new InvalidOperationException(
                        $"PO line {qcLine.PoLineId} was not found.");
                }


                PurchaseOrderDeliveryScheduleLine?
                    scheduleLine = null;

                if (rr.ScheduleId.HasValue)
                {
                    scheduleLine =
                        await _context
                            .PurchaseOrderDeliveryScheduleLines
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.ScheduleId ==
                                    rr.ScheduleId.Value &&
                                x.PoLineId ==
                                    qcLine.PoLineId);
                }


                var previousUnitPrice =
                    await GetPreviousUnitPriceAsync(
                        currentPoId:
                            po.PoId,

                        supplierId:
                            po.SupplierId,

                        materialId:
                            qcLine.MaterialId,

                        currentPoDate:
                            po.PoDate
                    );


                var line =
                    new SupplierPerformanceEvaluationLine
                    {
                        QcLineId =
                            qcLine.QcLineId,

                        RrLineId =
                            rrLine.RrLineId,

                        PoLineId =
                            qcLine.PoLineId,

                        ScheduleLineId =
                            scheduleLine?
                                .ScheduleLineId,

                        MaterialId =
                            qcLine.MaterialId,


                        ApprovedQty =
                            qcLine.AcceptedQty,

                        RejectedQty =
                            qcLine.RejectedQty,

                        TotalInspectedQty =
                            qcLine.ReceivedQty,


                        ScheduledDate =
                            schedule?.ScheduledDate,

                        ActualDeliveryDate =
                            rr.DeliveryDate,

                        ScheduledQty =
                            scheduleLine?.ScheduledQty
                            ?? rrLine.ReceiveQty,

                        DeliveredQty =
                            rrLine.ReceiveQty,


                        NewUnitPrice =
                            poLine.PoUnitPrice,

                        PreviousUnitPrice =
                            previousUnitPrice,


                        CoaPoints = 0m,
                        TermsPoints = 0m,
                        OtherPoints = 0m,

                        ReliabilityScore = 0m,
                        ReliabilityGrade = 0m,

                        CreatedBy =
                            updatedBy,

                        CreatedAt =
                            now
                    };


                CalculateQuality(line);

                CalculateDelivery(line);

                CalculateCost(line);


                line.TotalGrade =
                    RoundScore(
                        line.QualityGrade +
                        line.DeliveryGrade +
                        line.CostGrade +
                        line.ReliabilityGrade
                    );


                evaluation.Lines.Add(line);
            }


            // ============================================================
            // RECALCULATE AUTOMATIC HEADER SCORES
            // ============================================================

            RecalculateHeader(
                evaluation);


            // Reliability still requires Purchasing input,
            // therefore evaluation is not FINALIZED yet.
            evaluation.Status =
                "PENDING_PURCHASING";

            evaluation.UpdatedBy =
                updatedBy;

            evaluation.UpdatedAt =
                now;


            evaluation.WorkflowHistory.Add(
                new SupplierEvaluationWorkflowHistory
                {
                    EvaluationId =
                        evaluation.EvaluationId,

                    FromStatus =
                        "PENDING_DELIVERY",

                    ToStatus =
                        "PENDING_PURCHASING",

                    Action =
                        "DELIVERY_QC_COMPLETED",

                    ActionBy =
                        updatedBy,

                    ActionAt =
                        now,

                    Remarks =
                        $"Delivery completed. " +
                        $"RR {rr.RrNo}; QC {qc.QcNo}. " +
                        "Automatic Quality, Delivery and Cost " +
                        "scores were calculated."
                });


            return evaluation;
        }

        public async Task<SupplierPerformanceEvaluation>
    UpdateFromFinalReceivingAsync(
        int processingId,
        string updatedBy,
        DateTime now)
        {
            if (processingId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(processingId));
            }

            if (string.IsNullOrWhiteSpace(updatedBy))
            {
                throw new ArgumentException(
                    "UpdatedBy is required.",
                    nameof(updatedBy));
            }

            // ============================================================
            // LOAD RMW PROCESSING
            // ============================================================

            var processing =
                await _context.RmwProcessingHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.ProcessingId == processingId);

            if (processing == null)
            {
                throw new InvalidOperationException(
                    "RMW processing record was not found.");
            }

            if (processing.QcId <= 0)
            {
                throw new InvalidOperationException(
                    "Raw Material Evaluation reference is missing.");
            }

            var qc =
                await _context.QcInspectionHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.QcId == processing.QcId);

            if (qc == null)
            {
                throw new InvalidOperationException(
                    "Raw Material Evaluation was not found.");
            }

            // ============================================================
            // LOAD ORIGINAL INCOMING RECEIVING
            // ============================================================
            if (processing.IncomingReceivingId <= 0)
            {
                throw new InvalidOperationException(
                    "Incoming Receiving reference is missing.");
            }

            var incoming =
                await _context.IncomingReceivings
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.IncomingReceivingId ==
                        processing.IncomingReceivingId);


            if (incoming == null)
            {
                throw new InvalidOperationException(
                    "Incoming Receiving record was not found.");
            }

            // ============================================================
            // LOAD PO
            // ============================================================

            var po =
                await _context.PurchaseOrderHeaders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x =>
                        x.PoId == processing.PoId);

            if (po == null)
            {
                throw new InvalidOperationException(
                    "Purchase Order was not found.");
            }

            // ============================================================
            // LOAD DELIVERY SCHEDULE
            // ============================================================

            PurchaseOrderDeliverySchedule? schedule = null;

            if (incoming.ScheduleId > 0)
            {
                schedule =
                    await _context.PurchaseOrderDeliverySchedules
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.ScheduleId ==
                            incoming.ScheduleId);
            }

            // ============================================================
            // LOAD EXISTING SUPPLIER EVALUATION
            // ============================================================

            var evaluation =
                await _context.SupplierPerformanceEvaluations
                    .Include(x => x.Lines)
                    .Include(x => x.WorkflowHistory)
                    .FirstOrDefaultAsync(x =>
                        x.PoId == processing.PoId);

            if (evaluation == null)
            {
                throw new InvalidOperationException(
                    $"Supplier evaluation for PO {po.PoNo} was not found.");
            }

            if (string.Equals(
                evaluation.Status,
                "FINALIZED",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Finalized supplier evaluation cannot be changed.");
            }

            var oldStatus =
                evaluation.Status;

            // ============================================================
            // UPDATE REFERENCES
            // ============================================================

            evaluation.SupplierId =
                processing.SupplierId;

            evaluation.ScheduleId =
                incoming.ScheduleId;

            evaluation.QcId =
                qc.QcId;

            evaluation.DeliveryDate =
                incoming.DeliveryDate;

            // ============================================================
            // REBUILD AUTOMATIC SCORE LINES
            // ============================================================

            evaluation.Lines.Clear();

            foreach (var qcLine in qc.Lines)
            {
                var poLine =
                    po.Lines.FirstOrDefault(x =>
                        x.PoLineId ==
                        qcLine.PoLineId);

                if (poLine == null)
                {
                    throw new InvalidOperationException(
                        $"PO line {qcLine.PoLineId} was not found.");
                }

                var incomingLine =
                    incoming.Lines.FirstOrDefault(x =>
                        x.PoLineId ==
                        qcLine.PoLineId);

                if (incomingLine == null)
                {
                    throw new InvalidOperationException(
                        $"Incoming Receiving line for PO line " +
                        $"{qcLine.PoLineId} was not found.");
                }

                PurchaseOrderDeliveryScheduleLine?
                    scheduleLine = null;

                if (incoming.ScheduleId > 0)
                {
                    scheduleLine =
                        await _context
                            .PurchaseOrderDeliveryScheduleLines
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.ScheduleId ==
                                    incoming.ScheduleId &&
                                x.PoLineId ==
                                    qcLine.PoLineId);
                }

                var previousUnitPrice =
                    await GetPreviousUnitPriceAsync(
                        currentPoId:
                            po.PoId,

                        supplierId:
                            po.SupplierId,

                        materialId:
                            qcLine.MaterialId,

                        currentPoDate:
                            po.PoDate);

                var line =
                    new SupplierPerformanceEvaluationLine
                    {
                        QcLineId =
                            qcLine.QcLineId,

                        RrLineId =
                            null,

                        PoLineId =
                            qcLine.PoLineId,

                        ScheduleLineId =
                            scheduleLine?.ScheduleLineId,

                        MaterialId =
                            qcLine.MaterialId,

                        // QUALITY:
                        // Final Raw Material Evaluation result
                        ApprovedQty =
                            qcLine.AcceptedQty,

                        RejectedQty =
                            qcLine.RejectedQty,

                        TotalInspectedQty =
                            qcLine.ReceivedQty,

                        // DELIVERY:
                        // Original supplier delivery against schedule
                        ScheduledDate =
                            schedule?.ScheduledDate,

                        ActualDeliveryDate =
                            incoming.DeliveryDate,

                        ScheduledQty =
                            scheduleLine?.ScheduledQty
                            ?? incomingLine.DeliveredQty,

                        DeliveredQty =
                            incomingLine.DeliveredQty,

                        // COST:
                        // Current PO vs previous supplier/material PO
                        NewUnitPrice =
                            poLine.PoUnitPrice,

                        PreviousUnitPrice =
                            previousUnitPrice,

                        // RELIABILITY:
                        // Purchasing fills these later
                        CoaPoints = 0m,
                        TermsPoints = 0m,
                        OtherPoints = 0m,

                        ReliabilityScore = 0m,
                        ReliabilityGrade = 0m,

                        CreatedBy =
                            updatedBy.Trim(),

                        CreatedAt =
                            now
                    };

                CalculateQuality(line);
                CalculateDelivery(line);
                CalculateCost(line);

                line.TotalGrade =
                    RoundScore(
                        line.QualityGrade +
                        line.DeliveryGrade +
                        line.CostGrade +
                        line.ReliabilityGrade);

                evaluation.Lines.Add(line);
            }

            // ============================================================
            // RECALCULATE HEADER
            // ============================================================

            RecalculateHeader(evaluation);

            evaluation.Status =
                "PENDING_PURCHASING";

            // Do not give final performance rating yet.
            evaluation.PerformanceRating =
                "PENDING";

            evaluation.UpdatedBy =
                updatedBy.Trim();

            evaluation.UpdatedAt =
                now;

            // ============================================================
            // WORKFLOW HISTORY
            // ============================================================

            evaluation.WorkflowHistory.Add(
                new SupplierEvaluationWorkflowHistory
                {
                    EvaluationId =
                        evaluation.EvaluationId,

                    FromStatus =
                        oldStatus,

                    ToStatus =
                        "PENDING_PURCHASING",

                    Action =
                        "FINAL_RR_COMMITTED",

                    ActionBy =
                        updatedBy.Trim(),

                    ActionAt =
                        now,

                    Remarks =
                        "Final Receiving Report committed. " +
                        "Automatic Quality, Delivery and Cost " +
                        "scores were calculated. " +
                        "Waiting for Purchasing reliability assessment."
                });

            return evaluation;
        }

        private static string GetPerformanceRating(
            decimal totalScore)
        {
            if (totalScore >= 90m)
                return "EXCELLENT";

            if (totalScore >= 80m)
                return "VERY GOOD";

            if (totalScore >= 70m)
                return "GOOD";

            if (totalScore >= 60m)
                return "NEEDS IMPROVEMENT";

            return "POOR";
        }

        private static decimal NormalizeScore(
            decimal score)
        {
            return RoundScore(
                Math.Clamp(
                    score,
                    0m,
                    100m));
        }

        private static decimal RoundScore(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        public async Task<SupplierPerformanceEvaluation>
    UpdateWorkflowStageAsync(
        int poId,
        string newStatus,
        string action,
        string actionBy,
        DateTime now,
        string? remarks = null)
        {
            if (poId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(poId));
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                throw new ArgumentException(
                    "New status is required.",
                    nameof(newStatus));
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                throw new ArgumentException(
                    "Action is required.",
                    nameof(action));
            }

            if (string.IsNullOrWhiteSpace(actionBy))
            {
                throw new ArgumentException(
                    "Action By is required.",
                    nameof(actionBy));
            }

            var evaluation =
                await _context.SupplierPerformanceEvaluations
                    .Include(x => x.WorkflowHistory)
                    .FirstOrDefaultAsync(x =>
                        x.PoId == poId);

            if (evaluation == null)
            {
                throw new InvalidOperationException(
                    $"Supplier evaluation for PO ID {poId} was not found.");
            }

            if (string.Equals(
                evaluation.Status,
                "FINALIZED",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Finalized supplier evaluation cannot be changed.");
            }

            var oldStatus =
                evaluation.Status;

            evaluation.Status =
                newStatus.Trim().ToUpperInvariant();

            evaluation.UpdatedBy =
                actionBy.Trim();

            evaluation.UpdatedAt =
                now;

            evaluation.WorkflowHistory.Add(
                new SupplierEvaluationWorkflowHistory
                {
                    EvaluationId =
                        evaluation.EvaluationId,

                    FromStatus =
                        oldStatus,

                    ToStatus =
                        evaluation.Status,

                    Action =
                        action.Trim().ToUpperInvariant(),

                    ActionBy =
                        actionBy.Trim(),

                    ActionAt =
                        now,

                    Remarks =
                        remarks
                });

            return evaluation;
        }
    }
}