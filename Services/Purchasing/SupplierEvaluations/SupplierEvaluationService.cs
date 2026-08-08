using inventory_api.Data;
using inventory_api.DTOs.Purchasing.SupplierEvaluations;
using inventory_api.Models.SupplierEvaluation;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Purchasing.SupplierEvaluations
{
    public class SupplierEvaluationService
    {
        private readonly AppDbContext _context;
        private readonly SupplierEvaluationGenerationService _generationService;
        private readonly SupplierEvaluationScoringService _scoringService;

        public SupplierEvaluationService(
            AppDbContext context,
            SupplierEvaluationGenerationService generationService,
            SupplierEvaluationScoringService scoringService)
        {
            _context = context;
            _generationService = generationService;
            _scoringService = scoringService;
        }

        /// <summary>
        /// Creates one monthly evaluation for a supplier.
        /// Only one evaluation per supplier, year and month is allowed.
        /// </summary>
        public async Task<SupplierEvaluationResultDto> GenerateEvaluationAsync(
            GenerateSupplierEvaluationDto request)
        {
            ValidateGenerateRequest(request);

            var existingEvaluation = await _context
                .SupplierPerformanceEvaluations
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SupplierId == request.SupplierId &&
                    x.EvaluationYear == request.EvaluationYear &&
                    x.EvaluationMonth == request.EvaluationMonth);

            if (existingEvaluation != null)
            {
                return new SupplierEvaluationResultDto
                {
                    Success = false,
                    Message =
                        "An evaluation already exists for this supplier " +
                        "and evaluation month.",
                    EvaluationId = existingEvaluation.EvaluationId,
                    EvaluationNo = existingEvaluation.EvaluationNo,
                    Status = existingEvaluation.Status
                };
            }

            var generatedMetrics =
                await _generationService.GenerateAsync(
                    request.SupplierId,
                    request.EvaluationYear,
                    request.EvaluationMonth);

            /*
             * Reliability is manual and is initially zero.
             */
            var scoreResult =
    _scoringService.CalculateAllScores(
        generatedMetrics.Quality.QualityScore,
        generatedMetrics.Delivery.DeliveryScore,
        generatedMetrics.Cost.CostScore,
        request.ReliabilityScore);

            var strategy =
     _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    var now = DateTime.UtcNow;

                    var evaluationNo =
                        await GenerateEvaluationNumberAsync(
                            request.EvaluationYear,
                            request.EvaluationMonth);

                    var evaluation =
                        new SupplierPerformanceEvaluation
                        {
                            EvaluationNo = evaluationNo,

                            SupplierId = request.SupplierId,

                            EvaluationYear =
                                request.EvaluationYear,

                            EvaluationMonth =
                                request.EvaluationMonth,

                            PeriodStart =
                                generatedMetrics.PeriodStart,

                            PeriodEnd =
                                generatedMetrics.PeriodEnd,

                            QualityScore =
                                scoreResult.QualityScore,

                            QualityWeightedScore =
                                scoreResult.QualityWeightedScore,

                            OnTimeDeliveryScore =
                                scoreResult.DeliveryScore,

                            DeliveryWeightedScore =
                                scoreResult.DeliveryWeightedScore,

                            CostCompetitivenessScore =
                                scoreResult.CostScore,

                            CostWeightedScore =
                                scoreResult.CostWeightedScore,

                            ReliabilityScore =
                                scoreResult.ReliabilityScore,

                            ReliabilityWeightedScore =
                                scoreResult.ReliabilityWeightedScore,

                            TotalScore =
                                scoreResult.TotalScore,

                            PerformanceRating =
                                scoreResult.PerformanceRating,

                            Status = "GENERATED",

                            Remarks = request.Remarks,

                            GeneratedBy = request.GeneratedBy,
                            GeneratedAt = now,

                            CreatedBy = request.GeneratedBy,
                            CreatedAt = now,

                            UpdatedBy = request.GeneratedBy,
                            UpdatedAt = now
                        };

                    _context.SupplierPerformanceEvaluations
                        .Add(evaluation);

                    await _context.SaveChangesAsync();

                    evaluation.QualityMetric =
      CreateQualityMetric(
          evaluation.EvaluationId,
          generatedMetrics.Quality,
          request.GeneratedBy);

                    evaluation.DeliveryMetric =
                        CreateDeliveryMetric(
                            evaluation.EvaluationId,
                            generatedMetrics.Delivery,
                            request.GeneratedBy);

                    evaluation.CostMetric =
                        CreateCostMetric(
                            evaluation.EvaluationId,
                            generatedMetrics.Cost,
                            request.GeneratedBy);

                    evaluation.ReliabilityAssessment =
     CreateGeneratedReliabilityAssessment(
         evaluation.EvaluationId,
         request.ReliabilityScore,
         request.ReliabilityRemarks,
         request.GeneratedBy,
         now);

                    evaluation.WorkflowHistory.Add(
                        CreateWorkflowHistory(
                            evaluation.EvaluationId,
                          fromStatus: null,
toStatus: "GENERATED",
action: "GENERATED",
                            actionBy: request.GeneratedBy,
                            remarks: request.Remarks,
                            actionAt: now));

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return new SupplierEvaluationResultDto
                    {
                        Success = true,
                        Message =
                            "Supplier evaluation was generated successfully.",
                        EvaluationId = evaluation.EvaluationId,
                        EvaluationNo = evaluation.EvaluationNo,
                        Status = evaluation.Status
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        /// <summary>
        /// Recalculates automatic metrics of an existing GENERATED evaluation.
        /// Manual reliability values are preserved.
        /// Manual reliability values are preserved.
        /// </summary>
        /// 

        private static SupplierEvaluationReliabilityScore
    CreateGeneratedReliabilityAssessment(
        int evaluationId,
        decimal reliabilityScore,
        string? remarks,
        string scoredBy,
        DateTime scoredAt)
        {
            return new SupplierEvaluationReliabilityScore
            {
                EvaluationId = evaluationId,

                /*
                 * The current wizard provides one consolidated
                 * Reliability score instead of the old four-part
                 * reliability assessment.
                 */
                ResponsivenessScore = 0m,
                IssueResolutionScore = 0m,
                ReplacementSupportScore = 0m,
                CommunicationScore = 0m,

                ReliabilityScore = reliabilityScore,
                Remarks = remarks,

                ScoredBy = scoredBy,
                ScoredAt = scoredAt,

                UpdatedBy = scoredBy,
                UpdatedAt = scoredAt
            };
        }
        public async Task<SupplierEvaluationResultDto> RegenerateAsync(
            int evaluationId,
            SupplierEvaluationWorkflowActionDto request)
        {
            if (evaluationId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationId));
            }

            if (string.IsNullOrWhiteSpace(request.ActionBy))
            {
                throw new ArgumentException(
                    "ActionBy is required.",
                    nameof(request));
            }

            var evaluation = await _context
                .SupplierPerformanceEvaluations
                .Include(x => x.QualityMetric)
                .Include(x => x.DeliveryMetric)
                .Include(x => x.CostMetric)
                .Include(x => x.ReliabilityAssessment)
                .Include(x => x.WorkflowHistory)
                .FirstOrDefaultAsync(x =>
                    x.EvaluationId == evaluationId);

            if (evaluation == null)
            {
                return new SupplierEvaluationResultDto
                {
                    Success = false,
                    Message = "Supplier evaluation was not found."
                };
            }

            if (!string.Equals(
                   evaluation.Status,
"GENERATED",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new SupplierEvaluationResultDto
                {
                    Success = false,
                    Message =
                        "Only GENERATED evaluations can be regenerated.",
                    EvaluationId = evaluation.EvaluationId,
                    EvaluationNo = evaluation.EvaluationNo,
                    Status = evaluation.Status
                };
            }

            var generatedMetrics =
                await _generationService.GenerateAsync(
                    evaluation.SupplierId,
                    evaluation.EvaluationYear,
                    evaluation.EvaluationMonth);

            var reliabilityScore =
                evaluation.ReliabilityAssessment?.ReliabilityScore ?? 0m;

            var scoreResult = _scoringService.CalculateAllScores(
                generatedMetrics.Quality.QualityScore,
                generatedMetrics.Delivery.DeliveryScore,
                generatedMetrics.Cost.CostScore,
                reliabilityScore);
            var actionBy = request.ActionBy!;

            var strategy =
                _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    var now = DateTime.UtcNow;

                    UpdateEvaluationScores(
                        evaluation,
                        generatedMetrics,
                        scoreResult);

                    UpdateQualityMetric(
                        evaluation,
                        generatedMetrics.Quality,
                        actionBy);

                    UpdateDeliveryMetric(
                        evaluation,
                        generatedMetrics.Delivery,
                        actionBy);

                    UpdateCostMetric(
                        evaluation,
                        generatedMetrics.Cost,
                        actionBy);

                    evaluation.UpdatedBy = actionBy;
                    evaluation.UpdatedAt = now;

                    evaluation.WorkflowHistory.Add(
                        CreateWorkflowHistory(
                            evaluation.EvaluationId,
                         fromStatus: "GENERATED",
toStatus: "GENERATED",
action: "REGENERATED",
                            actionBy: actionBy,
                            remarks: request.Remarks,
                            actionAt: now));

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new SupplierEvaluationResultDto
                    {
                        Success = true,
                        Message =
                            "Supplier evaluation was regenerated successfully.",
                        EvaluationId = evaluation.EvaluationId,
                        EvaluationNo = evaluation.EvaluationNo,
                        Status = evaluation.Status
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

        }

        private static void ValidateGenerateRequest(
            GenerateSupplierEvaluationDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.SupplierId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.SupplierId),
                    "Supplier ID must be greater than zero.");
            }

            if (request.ReliabilityScore < 0m ||
    request.ReliabilityScore > 100m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.ReliabilityScore),
                    "Reliability score must be between 0 and 100.");
            }

            if (request.EvaluationYear < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.EvaluationYear),
                    "Evaluation year must be 2000 or later.");
            }

            if (request.EvaluationMonth is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.EvaluationMonth),
                    "Evaluation month must be between 1 and 12.");
            }

            if (string.IsNullOrWhiteSpace(request.GeneratedBy))
            {
                throw new ArgumentException(
                    "GeneratedBy is required.",
                    nameof(request.GeneratedBy));
            }
        }

        private async Task<string> GenerateEvaluationNumberAsync(
            int year,
            int month)
        {
            var prefix = $"SPE-{year}-{month:00}-";

            var existingNumbers = await _context
                .SupplierPerformanceEvaluations
                .AsNoTracking()
                .Where(x => x.EvaluationNo.StartsWith(prefix))
                .Select(x => x.EvaluationNo)
                .ToListAsync();

            var highestSequence = 0;

            foreach (var evaluationNo in existingNumbers)
            {
                var sequencePart = evaluationNo
                    .Replace(prefix, string.Empty);

                if (int.TryParse(
                        sequencePart,
                        out var sequence) &&
                    sequence > highestSequence)
                {
                    highestSequence = sequence;
                }
            }

            return $"{prefix}{highestSequence + 1:0000}";
        }
        private static SupplierEvaluationQualityMetric CreateQualityMetric(
       int evaluationId,
       GeneratedQualityMetric source,
       string calculatedBy)
        {
            return new SupplierEvaluationQualityMetric
            {
                EvaluationId = evaluationId,
                TotalReceivingReportCount = source.ReceivingReportCount,
                TotalQcCount = source.QcInspectionCount,
                TotalReceivedQty = source.TotalReceivedQty,
                TotalAcceptedQty = source.TotalAcceptedQty,
                TotalRejectedQty = source.TotalRejectedQty,
                AcceptanceRate = source.AcceptanceRate,
                RejectionRate = source.RejectionRate,
                QualityScore = source.QualityScore,
                CalculationNotes = source.CalculationRemarks,
                CalculatedAt = DateTime.UtcNow,
                CalculatedBy = calculatedBy
            };
        }
        private static SupplierEvaluationDeliveryMetric CreateDeliveryMetric(
     int evaluationId,
     GeneratedDeliveryMetric source,
     string calculatedBy)
        {
            return new SupplierEvaluationDeliveryMetric
            {
                EvaluationId = evaluationId,
                TotalScheduledDeliveries = source.ScheduledDeliveries,
                CompletedDeliveries = source.CompletedDeliveries,
                OnTimeDeliveries = source.OnTimeDeliveries,
                LateDeliveries = source.LateDeliveries,
                EarlyDeliveries = source.EarlyDeliveries,
                IncompleteDeliveries = source.IncompleteDeliveries,
                UndeliveredSchedules = source.UndeliveredSchedules,
                OnTimeRate = source.OnTimeDeliveryRate,
                AverageDelayDays = source.AverageDelayDays,
                DeliveryScore = source.DeliveryScore,
                CalculationNotes = source.CalculationRemarks,
                CalculatedAt = DateTime.UtcNow,
                CalculatedBy = calculatedBy
            };
        }

        private static SupplierEvaluationCostMetric CreateCostMetric(
    int evaluationId,
    GeneratedCostMetric source,
    string calculatedBy)
        {
            return new SupplierEvaluationCostMetric
            {
                EvaluationId = evaluationId,
                TotalPoCount = source.PurchaseOrderCount,
                TotalPoLineCount = source.PurchaseOrderLineCount,
                TotalPurchaseAmount = source.TotalPurchaseAmount,
                SupplierAverageUnitPrice = source.SupplierAverageUnitPrice,
                ComparisonAverageUnitPrice = source.ComparisonAverageUnitPrice,
                PriceVarianceAmount = source.PriceVarianceAmount,
                PriceVariancePercent = source.PriceVariancePercentage,

                LowestPriceLineCount = 0,
                ComparedLineCount = 0,

                CostScore = source.CostScore,
                CalculationNotes = source.CalculationRemarks,
                CalculatedAt = DateTime.UtcNow,
                CalculatedBy = calculatedBy
            };
        }

   

        private static SupplierEvaluationWorkflowHistory
            CreateWorkflowHistory(
                int evaluationId,
                string? fromStatus,
                string toStatus,
                string action,
                string actionBy,
                string? remarks,
                DateTime actionAt)
        {
            return new SupplierEvaluationWorkflowHistory
            {
                EvaluationId = evaluationId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                Action = action,
                ActionBy = actionBy,
                Remarks = remarks,
                ActionAt = actionAt
            };
        }

        private static void UpdateEvaluationScores(
            SupplierPerformanceEvaluation evaluation,
            SupplierEvaluationGeneratedMetrics metrics,
            SupplierEvaluationScoreResult scores)
        {
            evaluation.PeriodStart = metrics.PeriodStart;
            evaluation.PeriodEnd = metrics.PeriodEnd;

            evaluation.QualityScore =
                scores.QualityScore;

            evaluation.QualityWeightedScore =
                scores.QualityWeightedScore;

            evaluation.OnTimeDeliveryScore =
                scores.DeliveryScore;

            evaluation.DeliveryWeightedScore =
                scores.DeliveryWeightedScore;

            evaluation.CostCompetitivenessScore =
                scores.CostScore;

            evaluation.CostWeightedScore =
                scores.CostWeightedScore;

            evaluation.ReliabilityScore =
                scores.ReliabilityScore;

            evaluation.ReliabilityWeightedScore =
                scores.ReliabilityWeightedScore;

            evaluation.TotalScore =
                scores.TotalScore;

            evaluation.PerformanceRating =
                scores.PerformanceRating;
        }

        private static void UpdateQualityMetric(
      SupplierPerformanceEvaluation evaluation,
      GeneratedQualityMetric source,
      string calculatedBy)
        {
            evaluation.QualityMetric ??=
                CreateQualityMetric(
                    evaluation.EvaluationId,
                    source,
                    calculatedBy);

            evaluation.QualityMetric.TotalReceivingReportCount =
                source.ReceivingReportCount;

            evaluation.QualityMetric.TotalQcCount =
                source.QcInspectionCount;

            evaluation.QualityMetric.TotalReceivedQty =
                source.TotalReceivedQty;

            evaluation.QualityMetric.TotalAcceptedQty =
                source.TotalAcceptedQty;

            evaluation.QualityMetric.TotalRejectedQty =
                source.TotalRejectedQty;

            evaluation.QualityMetric.AcceptanceRate =
                source.AcceptanceRate;

            evaluation.QualityMetric.RejectionRate =
                source.RejectionRate;

            evaluation.QualityMetric.QualityScore =
                source.QualityScore;

            evaluation.QualityMetric.CalculationNotes =
                source.CalculationRemarks;

            evaluation.QualityMetric.CalculatedAt =
                DateTime.UtcNow;

            evaluation.QualityMetric.CalculatedBy =
                calculatedBy;
        }
        private static void UpdateDeliveryMetric(
         SupplierPerformanceEvaluation evaluation,
         GeneratedDeliveryMetric source,
         string calculatedBy)
        {
            evaluation.DeliveryMetric ??=
                CreateDeliveryMetric(
                    evaluation.EvaluationId,
                    source,
                    calculatedBy);

            evaluation.DeliveryMetric.TotalScheduledDeliveries =
                source.ScheduledDeliveries;

            evaluation.DeliveryMetric.CompletedDeliveries =
                source.CompletedDeliveries;

            evaluation.DeliveryMetric.OnTimeDeliveries =
                source.OnTimeDeliveries;

            evaluation.DeliveryMetric.LateDeliveries =
                source.LateDeliveries;

            evaluation.DeliveryMetric.EarlyDeliveries =
                source.EarlyDeliveries;

            evaluation.DeliveryMetric.IncompleteDeliveries =
                source.IncompleteDeliveries;

            evaluation.DeliveryMetric.UndeliveredSchedules =
                source.UndeliveredSchedules;

            evaluation.DeliveryMetric.OnTimeRate =
                source.OnTimeDeliveryRate;

            evaluation.DeliveryMetric.AverageDelayDays =
                source.AverageDelayDays;

            evaluation.DeliveryMetric.DeliveryScore =
                source.DeliveryScore;

            evaluation.DeliveryMetric.CalculationNotes =
                source.CalculationRemarks;

            evaluation.DeliveryMetric.CalculatedAt =
                DateTime.UtcNow;

            evaluation.DeliveryMetric.CalculatedBy =
                calculatedBy;
        }
        private static void UpdateCostMetric(
      SupplierPerformanceEvaluation evaluation,
      GeneratedCostMetric source,
      string calculatedBy)
        {
            evaluation.CostMetric ??=
                CreateCostMetric(
                    evaluation.EvaluationId,
                    source,
                    calculatedBy);

            evaluation.CostMetric.TotalPoCount =
                source.PurchaseOrderCount;

            evaluation.CostMetric.TotalPoLineCount =
                source.PurchaseOrderLineCount;

            evaluation.CostMetric.TotalPurchaseAmount =
                source.TotalPurchaseAmount;

            evaluation.CostMetric.SupplierAverageUnitPrice =
                source.SupplierAverageUnitPrice;

            evaluation.CostMetric.ComparisonAverageUnitPrice =
                source.ComparisonAverageUnitPrice;

            evaluation.CostMetric.PriceVarianceAmount =
                source.PriceVarianceAmount;

            evaluation.CostMetric.PriceVariancePercent =
                source.PriceVariancePercentage;

            evaluation.CostMetric.CostScore =
                source.CostScore;

            evaluation.CostMetric.CalculationNotes =
                source.CalculationRemarks;

            evaluation.CostMetric.CalculatedAt =
                DateTime.UtcNow;

            evaluation.CostMetric.CalculatedBy =
                calculatedBy;
        }

   
      
     
        private static SupplierEvaluationResultDto CreateSuccessResult(
    SupplierPerformanceEvaluation evaluation,
    string message)
        {
            return new SupplierEvaluationResultDto
            {
                Success = true,
                Message = message,
                EvaluationId = evaluation.EvaluationId,
                EvaluationNo = evaluation.EvaluationNo,
                Status = evaluation.Status
            };
        }
        private static SupplierEvaluationResultDto CreateFailedResult(
    string message,
    SupplierPerformanceEvaluation? evaluation = null)
        {
            return new SupplierEvaluationResultDto
            {
                Success = false,
                Message = message,
                EvaluationId = evaluation?.EvaluationId,
                EvaluationNo = evaluation?.EvaluationNo,
                Status = evaluation?.Status
            };
        }


        public Task<SupplierEvaluationResultDto> FinalizeAsync(
       int evaluationId,
       SupplierEvaluationWorkflowActionDto request)
        {
            return ChangeStatusAsync(
                evaluationId,
                expectedStatus: "GENERATED",
                newStatus: "FINALIZED",
                action: "FINALIZED",
                actionBy: request.ActionBy,
                remarks: request.Remarks,
                applyAdditionalChanges: (evaluation, now) =>
                {
                    evaluation.FinalizedBy = request.ActionBy;
                    evaluation.FinalizedAt = now;
                });
        }
        private async Task<SupplierEvaluationResultDto> ChangeStatusAsync(
     int evaluationId,
     string expectedStatus,
     string newStatus,
     string action,
     string actionBy,
     string? remarks,
     Action<SupplierPerformanceEvaluation, DateTime>? applyAdditionalChanges = null)
        {
            if (evaluationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(evaluationId));
            }

            if (string.IsNullOrWhiteSpace(actionBy))
            {
                throw new ArgumentException(
                    "ActionBy is required.",
                    nameof(actionBy));
            }

            var evaluation = await _context
                .SupplierPerformanceEvaluations
                .Include(x => x.WorkflowHistory)
                .FirstOrDefaultAsync(x =>
                    x.EvaluationId == evaluationId);

            if (evaluation == null)
            {
                return CreateFailedResult(
                    "Supplier evaluation was not found.");
            }

            if (!string.Equals(
                    evaluation.Status,
                    expectedStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                return CreateFailedResult(
                    $"Only {expectedStatus} evaluations can be changed to {newStatus}.",
                    evaluation);
            }

            var now = DateTime.UtcNow;

            var oldStatus = evaluation.Status;

            evaluation.Status = newStatus;
            evaluation.UpdatedBy = actionBy;
            evaluation.UpdatedAt = now;

            evaluation.WorkflowHistory.Add(
                CreateWorkflowHistory(
                    evaluation.EvaluationId,
                    fromStatus: oldStatus,
                    toStatus: newStatus,
                    action: action,
                    actionBy: actionBy,
                    remarks: remarks,
                    actionAt: now));

            applyAdditionalChanges?.Invoke(
    evaluation,
    now);

            await _context.SaveChangesAsync();

            return CreateSuccessResult(
                evaluation,
                $"Supplier evaluation status changed to {newStatus}.");
        }

        public async Task<SupplierEvaluationDetailsDto?> GetDetailsAsync(
    int evaluationId)
        {
            if (evaluationId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationId));
            }

            var evaluation = await _context
    .SupplierPerformanceEvaluations
    .AsNoTracking()
    .Include(x => x.QualityMetric)
    .Include(x => x.DeliveryMetric)
    .Include(x => x.CostMetric)
    .Include(x => x.ReliabilityAssessment)
    .Include(x => x.WorkflowHistory)
    .FirstOrDefaultAsync(x =>
        x.EvaluationId == evaluationId);

            if (evaluation == null)
            {
                return null;
            }

            return new SupplierEvaluationDetailsDto
            {
                EvaluationId = evaluation.EvaluationId,
                EvaluationNo = evaluation.EvaluationNo,

                SupplierId = evaluation.SupplierId,

                SupplierCode = string.Empty,
                SupplierName = string.Empty,
                SupplierType = null,
                ContactPerson = null,

                EvaluationYear = evaluation.EvaluationYear,
                EvaluationMonth = evaluation.EvaluationMonth,

                EvaluationMonthName = new DateTime(
                    evaluation.EvaluationYear,
                    evaluation.EvaluationMonth,
                    1).ToString("MMMM"),

                PeriodStart = evaluation.PeriodStart,
                PeriodEnd = evaluation.PeriodEnd,

                QualityScore = evaluation.QualityScore,
                QualityWeightedScore = evaluation.QualityWeightedScore,

                OnTimeDeliveryScore = evaluation.OnTimeDeliveryScore,
                DeliveryWeightedScore = evaluation.DeliveryWeightedScore,

                CostCompetitivenessScore =
                    evaluation.CostCompetitivenessScore,

                CostWeightedScore =
                    evaluation.CostWeightedScore,

                ReliabilityScore =
                    evaluation.ReliabilityScore,

                ReliabilityWeightedScore =
                    evaluation.ReliabilityWeightedScore,

                TotalScore = evaluation.TotalScore,
                PerformanceRating = evaluation.PerformanceRating,

                Status = evaluation.Status,
                Remarks = evaluation.Remarks,

                GeneratedBy = evaluation.GeneratedBy,
                GeneratedAt = evaluation.GeneratedAt,

                ReviewedBy = evaluation.ReviewedBy,
                ReviewedAt = evaluation.ReviewedAt,

                ApprovedBy = evaluation.ApprovedBy,
                ApprovedAt = evaluation.ApprovedAt,

                FinalizedBy = evaluation.FinalizedBy,
                FinalizedAt = evaluation.FinalizedAt,

                CreatedAt = evaluation.CreatedAt,
                UpdatedAt = evaluation.UpdatedAt,

                QualityMetric =
                    MapQualityMetric(evaluation.QualityMetric),

                DeliveryMetric =
                    MapDeliveryMetric(evaluation.DeliveryMetric),

                CostMetric =
                    MapCostMetric(evaluation.CostMetric),

                ReliabilityAssessment =
                    MapReliabilityAssessment(
                        evaluation.ReliabilityAssessment),

                WorkflowHistory = evaluation.WorkflowHistory
                    .OrderBy(x => x.ActionAt)
                    .Select(MapWorkflowHistory)
                    .ToList()
            };
        }

        private static SupplierEvaluationQualityMetricDto?
    MapQualityMetric(
        SupplierEvaluationQualityMetric? metric)
        {
            if (metric == null)
            {
                return null;
            }

            return new SupplierEvaluationQualityMetricDto
            {
                QualityMetricId = metric.QualityMetricId,
                EvaluationId = metric.EvaluationId,

                ReceivingReportCount =
                    metric.TotalReceivingReportCount,

                QcInspectionCount =
                    metric.TotalQcCount,

                TotalReceivedQty =
                    metric.TotalReceivedQty,

                TotalAcceptedQty =
                    metric.TotalAcceptedQty,

                TotalRejectedQty =
                    metric.TotalRejectedQty,

                AcceptanceRate =
                    metric.AcceptanceRate,

                RejectionRate =
                    metric.RejectionRate,

                QualityScore =
                    metric.QualityScore,

                CalculationRemarks =
                    metric.CalculationNotes
            };
        }

        private static SupplierEvaluationDeliveryMetricDto?
    MapDeliveryMetric(
        SupplierEvaluationDeliveryMetric? metric)
        {
            if (metric == null)
            {
                return null;
            }

            return new SupplierEvaluationDeliveryMetricDto
            {
                DeliveryMetricId = metric.DeliveryMetricId,
                EvaluationId = metric.EvaluationId,

                ScheduledDeliveries =
                    metric.TotalScheduledDeliveries,

                CompletedDeliveries =
                    metric.CompletedDeliveries,

                OnTimeDeliveries =
                    metric.OnTimeDeliveries,

                LateDeliveries =
                    metric.LateDeliveries,

                EarlyDeliveries =
                    metric.EarlyDeliveries,

                IncompleteDeliveries =
                    metric.IncompleteDeliveries,

                UndeliveredSchedules =
                    metric.UndeliveredSchedules,

                OnTimeDeliveryRate =
                    metric.OnTimeRate,

                AverageDelayDays =
                    metric.AverageDelayDays,

                DeliveryScore =
                    metric.DeliveryScore,

                CalculationRemarks =
                    metric.CalculationNotes
            };
        }

        private static SupplierEvaluationCostMetricDto?
    MapCostMetric(
        SupplierEvaluationCostMetric? metric)
        {
            if (metric == null)
            {
                return null;
            }

            return new SupplierEvaluationCostMetricDto
            {
                CostMetricId = metric.CostMetricId,
                EvaluationId = metric.EvaluationId,

                PurchaseOrderCount =
                    metric.TotalPoCount,

                PurchaseOrderLineCount =
                    metric.TotalPoLineCount,

                TotalPurchaseAmount =
                    metric.TotalPurchaseAmount,

                SupplierAverageUnitPrice =
                    metric.SupplierAverageUnitPrice,

                ComparisonAverageUnitPrice =
                    metric.ComparisonAverageUnitPrice,

                PriceVarianceAmount =
                    metric.PriceVarianceAmount,

                PriceVariancePercentage =
                    metric.PriceVariancePercent,

                CostScore =
                    metric.CostScore,

                CalculationRemarks =
                    metric.CalculationNotes
            };
        }

        private static SupplierEvaluationReliabilityDto?
    MapReliabilityAssessment(
        SupplierEvaluationReliabilityScore? reliability)
        {
            if (reliability == null)
            {
                return null;
            }

            return new SupplierEvaluationReliabilityDto
            {
                ReliabilityScoreId =
                    reliability.ReliabilityScoreId,

                EvaluationId =
                    reliability.EvaluationId,

                ResponsivenessScore =
                    reliability.ResponsivenessScore,

                IssueResolutionScore =
                    reliability.IssueResolutionScore,

                ReplacementSupportScore =
                    reliability.ReplacementSupportScore,

                CommunicationScore =
                    reliability.CommunicationScore,

                ReliabilityScore =
                    reliability.ReliabilityScore,

                Remarks =
                    reliability.Remarks,

                ScoredBy =
                    reliability.ScoredBy,

                ScoredAt =
                    reliability.ScoredAt
            };
        }

        private static SupplierEvaluationWorkflowHistoryDto
    MapWorkflowHistory(
        SupplierEvaluationWorkflowHistory history)
        {
            return new SupplierEvaluationWorkflowHistoryDto
            {
                HistoryId = history.HistoryId,
                EvaluationId = history.EvaluationId,

                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,

                Action = history.Action,
                Remarks = history.Remarks,

                ActionBy = history.ActionBy,
                ActionAt = history.ActionAt
            };
        }


        public async Task<SupplierEvaluationMonthlySummaryDto>
            GetMonthlySummaryAsync(
                int year,
                int month)
        {
            if (year < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Year must be 2000 or later.");
            }

            if (month is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "Month must be between 1 and 12.");
            }

            var evaluations = await _context
                .SupplierPerformanceEvaluations
                .AsNoTracking()
                .Where(x =>
                    x.EvaluationYear == year &&
                    x.EvaluationMonth == month)
                .OrderBy(x => x.EvaluationNo)
                .ToListAsync();

            var supplierIds = evaluations
                .Select(x => x.SupplierId)
                .Distinct()
                .ToList();

            var suppliers = await _context
                .Suppliers
                .AsNoTracking()
                .Where(x =>
                    supplierIds.Contains(x.SupplierId))
                .Select(x => new
                {
                    x.SupplierId,
                    x.SupplierCode,
                    x.SupplierName
                })
                .ToDictionaryAsync(
                    x => x.SupplierId,
                    x => x);

            var totalEvaluations =
                evaluations.Count;

            return new SupplierEvaluationMonthlySummaryDto
            {
                EvaluationYear = year,
                EvaluationMonth = month,

                EvaluationMonthName =
                    new DateTime(year, month, 1)
                        .ToString("MMMM"),

                TotalSuppliers = evaluations
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .Count(),

                TotalEvaluations =
                    totalEvaluations,

                GeneratedCount =
                    evaluations.Count(x =>
                        x.Status == "GENERATED"),

                FinalizedCount =
                    evaluations.Count(x =>
                        x.Status == "FINALIZED"),

                AverageQualityScore =
                    totalEvaluations == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.QualityScore),
                            2),

                AverageDeliveryScore =
                    totalEvaluations == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.OnTimeDeliveryScore),
                            2),

                AverageCostScore =
                    totalEvaluations == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.CostCompetitivenessScore),
                            2),

                AverageReliabilityScore =
                    totalEvaluations == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.ReliabilityScore),
                            2),

                AverageTotalScore =
                    totalEvaluations == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.TotalScore),
                            2),

                ExcellentCount =
                    evaluations.Count(x =>
                        x.PerformanceRating == "EXCELLENT"),

                VeryGoodCount =
                    evaluations.Count(x =>
                        x.PerformanceRating == "VERY GOOD"),

                GoodCount =
                    evaluations.Count(x =>
                        x.PerformanceRating == "GOOD"),

                NeedsImprovementCount =
                    evaluations.Count(x =>
                        x.PerformanceRating ==
                        "NEEDS IMPROVEMENT"),

                PoorCount =
                    evaluations.Count(x =>
                        x.PerformanceRating == "POOR"),

                Evaluations = evaluations
                    .Select(evaluation =>
                    {
                        suppliers.TryGetValue(
                            evaluation.SupplierId,
                            out var supplier);

                        return MapEvaluationList(
                            evaluation,
                            supplier?.SupplierCode ??
                                string.Empty,
                            supplier?.SupplierName ??
                                string.Empty);
                    })
                    .ToList()
            };
        }

        private static SupplierEvaluationListDto
      MapEvaluationList(
          SupplierPerformanceEvaluation evaluation,
          string supplierCode,
          string supplierName)
        {
            return new SupplierEvaluationListDto
            {
                EvaluationId =
                    evaluation.EvaluationId,

                EvaluationNo =
                    evaluation.EvaluationNo,

                SupplierId =
                    evaluation.SupplierId,

                SupplierCode =
                    supplierCode,

                SupplierName =
                    supplierName,

                EvaluationYear =
                    evaluation.EvaluationYear,

                EvaluationMonth =
                    evaluation.EvaluationMonth,

                EvaluationMonthName =
                    new DateTime(
                        evaluation.EvaluationYear,
                        evaluation.EvaluationMonth,
                        1)
                    .ToString("MMMM"),

                PeriodStart =
                    evaluation.PeriodStart,

                PeriodEnd =
                    evaluation.PeriodEnd,

                QualityScore =
                    evaluation.QualityScore,

                OnTimeDeliveryScore =
                    evaluation.OnTimeDeliveryScore,

                CostCompetitivenessScore =
                    evaluation.CostCompetitivenessScore,

                ReliabilityScore =
                    evaluation.ReliabilityScore,

                TotalScore =
                    evaluation.TotalScore,

                PerformanceRating =
                    evaluation.PerformanceRating,

                Status =
                    evaluation.Status,

                CreatedAt =
                    evaluation.CreatedAt,

                GeneratedBy =
                    evaluation.GeneratedBy
            };
        }


        public async Task<SupplierEvaluationGeneratedMetrics>
    PreviewEvaluationAsync(
        int supplierId,
        int evaluationYear,
        int evaluationMonth)
        {
            if (supplierId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(supplierId),
                    "Supplier ID must be greater than zero.");
            }

            return await _generationService.GenerateAsync(
                supplierId,
                evaluationYear,
                evaluationMonth);
        }
        



    }
}