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

        public SupplierEvaluationService(
            AppDbContext context,
            SupplierEvaluationGenerationService generationService)
        {
            _context = context;
            _generationService = generationService;
        }

        public async Task<List<SupplierEvaluationListDto>> GetAllAsync(
            SupplierEvaluationFilterDto? filter = null)
        {
            var query = _context.SupplierPerformanceEvaluations
                .AsNoTracking()
                .AsQueryable();

            if (filter?.SupplierId.HasValue == true)
            {
                query = query.Where(x =>
                    x.SupplierId == filter.SupplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter?.Status))
            {
                var status = filter.Status.Trim();

                query = query.Where(x =>
                    x.Status == status);
            }

            if (filter?.EvaluationYear.HasValue == true)
            {
                var year = filter.EvaluationYear.Value;

                query = query.Where(x =>
                    x.EvaluationDate.HasValue &&
                    x.EvaluationDate.Value.Year == year);
            }

            if (filter?.EvaluationMonth.HasValue == true)
            {
                var month = filter.EvaluationMonth.Value;

                query = query.Where(x =>
                    x.EvaluationDate.HasValue &&
                    x.EvaluationDate.Value.Month == month);
            }

            var evaluations = await query
                .OrderByDescending(x => x.EvaluationId)
                .ToListAsync();

            var supplierIds = evaluations
                .Select(x => x.SupplierId)
                .Distinct()
                .ToList();

            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(x => supplierIds.Contains(x.SupplierId))
                .Select(x => new
                {
                    x.SupplierId,
                    x.SupplierCode,
                    x.SupplierName
                })
                .ToDictionaryAsync(
                    x => x.SupplierId,
                    x => x);

            var poIds = evaluations
                .Where(x => x.PoId.HasValue)
                .Select(x => x.PoId!.Value)
                .Distinct()
                .ToList();

            var pos = await _context.PurchaseOrderHeaders
                .AsNoTracking()
                .Where(x => poIds.Contains(x.PoId))
                .Select(x => new
                {
                    x.PoId,
                    x.PoNo,
                    x.PrintedPoNo
                })
                .ToDictionaryAsync(
                    x => x.PoId,
                    x => x);

            var rrIds = evaluations
                .Where(x => x.RrId.HasValue)
                .Select(x => x.RrId!.Value)
                .Distinct()
                .ToList();

            var rrs = await _context.ReceivingReportHeaders
                .AsNoTracking()
                .Where(x => rrIds.Contains(x.RrId))
                .Select(x => new
                {
                    x.RrId,
                    x.RrNo
                })
                .ToDictionaryAsync(
                    x => x.RrId,
                    x => x);

            var qcIds = evaluations
                .Where(x => x.QcId.HasValue)
                .Select(x => x.QcId!.Value)
                .Distinct()
                .ToList();

            var qcs = await _context.QcInspectionHeaders
                .AsNoTracking()
                .Where(x => qcIds.Contains(x.QcId))
                .Select(x => new
                {
                    x.QcId,
                    x.QcNo
                })
                .ToDictionaryAsync(
                    x => x.QcId,
                    x => x);

            return evaluations
                .Select(evaluation =>
                {
                    suppliers.TryGetValue(
                        evaluation.SupplierId,
                        out var supplier);

                    object? po = null;
                    object? rr = null;
                    object? qc = null;

                    string poNo = string.Empty;
                    string rrNo = string.Empty;
                    string qcNo = string.Empty;

                    if (evaluation.PoId.HasValue &&
                        pos.TryGetValue(
                            evaluation.PoId.Value,
                            out var poRecord))
                    {
                        poNo =
                            !string.IsNullOrWhiteSpace(
                                poRecord.PrintedPoNo)
                                ? poRecord.PrintedPoNo
                                : poRecord.PoNo;
                    }

                    if (evaluation.RrId.HasValue &&
                        rrs.TryGetValue(
                            evaluation.RrId.Value,
                            out var rrRecord))
                    {
                        rrNo = rrRecord.RrNo;
                    }

                    if (evaluation.QcId.HasValue &&
                        qcs.TryGetValue(
                            evaluation.QcId.Value,
                            out var qcRecord))
                    {
                        qcNo = qcRecord.QcNo;
                    }

                    return new SupplierEvaluationListDto
                    {
                        EvaluationId =
                            evaluation.EvaluationId,

                        EvaluationNo =
                            evaluation.EvaluationNo,

                        SupplierId =
                            evaluation.SupplierId,

                        SupplierCode =
                            supplier?.SupplierCode ??
                            string.Empty,

                        SupplierName =
                            supplier?.SupplierName ??
                            string.Empty,

                        PoId =
                            evaluation.PoId,

                        PoNo =
                            poNo,

                        RrId =
                            evaluation.RrId,

                        RrNo =
                            rrNo,

                        QcId =
                            evaluation.QcId,

                        QcNo =
                            qcNo,

                        EvaluationDate =
                            evaluation.EvaluationDate,

                        DeliveryDate =
                            evaluation.DeliveryDate,

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
                            evaluation.PerformanceRating ??
                            string.Empty,

                        Status =
                            evaluation.Status,

                        CreatedAt =
                            evaluation.CreatedAt,

                        GeneratedBy =
                            evaluation.GeneratedBy
                    };
                })
                .ToList();
        }

        public async Task<SupplierEvaluationDetailsDto?>
            GetDetailsAsync(
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
                .Include(x => x.Lines)
                .Include(x => x.WorkflowHistory)
                .FirstOrDefaultAsync(x =>
                    x.EvaluationId == evaluationId);

            if (evaluation == null)
            {
                return null;
            }

            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SupplierId ==
                    evaluation.SupplierId);

            var po = evaluation.PoId.HasValue
                ? await _context.PurchaseOrderHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.PoId ==
                        evaluation.PoId.Value)
                : null;

            var rr = evaluation.RrId.HasValue
                ? await _context.ReceivingReportHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.RrId ==
                        evaluation.RrId.Value)
                : null;

            var qc = evaluation.QcId.HasValue
                ? await _context.QcInspectionHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.QcId ==
                        evaluation.QcId.Value)
                : null;

            var materialIds = evaluation.Lines
                .Select(x => x.MaterialId)
                .Distinct()
                .ToList();

            var materials = await _context.Materials
                .AsNoTracking()
                .Where(x =>
                    materialIds.Contains(x.material_id))
                .Select(x => new
                {
                    x.material_id,
                    x.material_code,
                    x.material_name
                })
                .ToDictionaryAsync(
                    x => x.material_id,
                    x => x);

            return new SupplierEvaluationDetailsDto
            {
                EvaluationId =
                    evaluation.EvaluationId,

                EvaluationNo =
                    evaluation.EvaluationNo,

                SupplierId =
                    evaluation.SupplierId,

                SupplierCode =
                    supplier?.SupplierCode ??
                    string.Empty,

                SupplierName =
                    supplier?.SupplierName ??
                    string.Empty,

                SupplierType =
                    supplier?.SupplierType,

                ContactPerson =
                    supplier?.ContactPerson,

                PoId =
                    evaluation.PoId,

                PoNo =
                    po == null
                        ? string.Empty
                        : !string.IsNullOrWhiteSpace(
                            po.PrintedPoNo)
                            ? po.PrintedPoNo
                            : po.PoNo,

                ScheduleId =
                    evaluation.ScheduleId,

                RrId =
                    evaluation.RrId,

                RrNo =
                    rr?.RrNo ??
                    string.Empty,

                QcId =
                    evaluation.QcId,

                QcNo =
                    qc?.QcNo ??
                    string.Empty,

                EvaluationDate =
                    evaluation.EvaluationDate,

                DeliveryDate =
                    evaluation.DeliveryDate,

                QualityScore =
                    evaluation.QualityScore,

                QualityWeightedScore =
                    evaluation.QualityWeightedScore,

                OnTimeDeliveryScore =
                    evaluation.OnTimeDeliveryScore,

                DeliveryWeightedScore =
                    evaluation.DeliveryWeightedScore,

                CostCompetitivenessScore =
                    evaluation.CostCompetitivenessScore,

                CostWeightedScore =
                    evaluation.CostWeightedScore,

                ReliabilityScore =
                    evaluation.ReliabilityScore,

                ReliabilityWeightedScore =
                    evaluation.ReliabilityWeightedScore,

                TotalScore =
                    evaluation.TotalScore,

                PerformanceRating =
                    evaluation.PerformanceRating ??
                    string.Empty,

                Status =
                    evaluation.Status,

                Remarks =
                    evaluation.Remarks,

                GeneratedBy =
                    evaluation.GeneratedBy,

                GeneratedAt =
                    evaluation.GeneratedAt,

                FinalizedBy =
                    evaluation.FinalizedBy,

                FinalizedAt =
                    evaluation.FinalizedAt,

                CreatedAt =
                    evaluation.CreatedAt,

                UpdatedAt =
                    evaluation.UpdatedAt,

                Lines = evaluation.Lines
                    .OrderBy(x =>
                        x.EvaluationLineId)
                    .Select(line =>
                    {
                        materials.TryGetValue(
                            line.MaterialId,
                            out var material);

                        return new SupplierEvaluationLineDto
                        {
                            EvaluationLineId =
                                line.EvaluationLineId,

                            EvaluationId =
                                line.EvaluationId,

                            QcLineId =
                                line.QcLineId,

                            RrLineId =
                                line.RrLineId,

                            PoLineId =
                                line.PoLineId,

                            ScheduleLineId =
                                line.ScheduleLineId,

                            MaterialId =
                                line.MaterialId,

                            MaterialCode =
                                material?.material_code ??
                                string.Empty,

                            MaterialName =
                                material?.material_name ??
                                string.Empty,

                            ApprovedQty =
                                line.ApprovedQty,

                            RejectedQty =
                                line.RejectedQty,

                            TotalInspectedQty =
                                line.TotalInspectedQty,

                            QualityScore =
                                line.QualityScore,

                            QualityGrade =
                                line.QualityGrade,

                            ScheduledDate =
                                line.ScheduledDate,

                            ActualDeliveryDate =
                                line.ActualDeliveryDate,

                            IsOnTime =
                                line.IsOnTime,

                            OnTimeScore =
                                line.OnTimeScore,

                            ScheduledQty =
                                line.ScheduledQty,

                            DeliveredQty =
                                line.DeliveredQty,

                            InFullScore =
                                line.InFullScore,

                            DeliveryScore =
                                line.DeliveryScore,

                            DeliveryGrade =
                                line.DeliveryGrade,

                            NewUnitPrice =
                                line.NewUnitPrice,

                            PreviousUnitPrice =
                                line.PreviousUnitPrice,

                            PriceChangePercent =
                                line.PriceChangePercent,

                            CostStatus =
                                line.CostStatus,

                            CostScore =
                                line.CostScore,

                            CostGrade =
                                line.CostGrade,

                            CoaPoints =
                                line.CoaPoints,

                            TermsPoints =
                                line.TermsPoints,

                            OtherPoints =
                                line.OtherPoints,

                            ReliabilityScore =
                                line.ReliabilityScore,

                            ReliabilityGrade =
                                line.ReliabilityGrade,

                            TotalGrade =
                                line.TotalGrade,

                            Remarks =
                                line.Remarks
                        };
                    })
                    .ToList(),

                WorkflowHistory =
                    evaluation.WorkflowHistory
                        .OrderBy(x => x.ActionAt)
                        .Select(x =>
                            new SupplierEvaluationWorkflowHistoryDto
                            {
                                HistoryId =
                                    x.HistoryId,

                                EvaluationId =
                                    x.EvaluationId,

                                FromStatus =
                                    x.FromStatus,

                                ToStatus =
                                    x.ToStatus,

                                Action =
                                    x.Action,

                                Remarks =
                                    x.Remarks,

                                ActionBy =
                                    x.ActionBy,

                                ActionAt =
                                    x.ActionAt
                            })
                        .ToList()
            };
        }

        public async Task<SupplierEvaluationResultDto>
            SaveReliabilityAsync(
                int evaluationId,
                SaveSupplierEvaluationReliabilityDto request)
        {
            if (evaluationId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationId));
            }

            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(
                request.UpdatedBy))
            {
                throw new ArgumentException(
                    "UpdatedBy is required.",
                    nameof(request));
            }

            var evaluation = await _context
                .SupplierPerformanceEvaluations
                .Include(x => x.Lines)
                .Include(x => x.WorkflowHistory)
                .FirstOrDefaultAsync(x =>
                    x.EvaluationId ==
                    evaluationId);

            if (evaluation == null)
            {
                return CreateFailedResult(
                    "Supplier evaluation was not found.");
            }

            if (!string.Equals(
                evaluation.Status,
                "PENDING_PURCHASING",
                StringComparison.OrdinalIgnoreCase))
            {
                return CreateFailedResult(
                    "Only PENDING_PURCHASING evaluations can be updated.",
                    evaluation);
            }

            var now = DateTime.UtcNow;

            foreach (var lineRequest in request.Lines)
            {
                var line = evaluation.Lines
                    .FirstOrDefault(x =>
                        x.EvaluationLineId ==
                        lineRequest.EvaluationLineId);

                if (line == null)
                {
                    throw new InvalidOperationException(
                        $"Evaluation line " +
                        $"{lineRequest.EvaluationLineId} " +
                        "was not found.");
                }

                ValidateReliabilityPoints(
                    lineRequest);

                line.CoaPoints =
                    lineRequest.CoaPoints;

                line.TermsPoints =
                    lineRequest.TermsPoints;

                line.OtherPoints =
                    lineRequest.OtherPoints;

                /*
                 * Maximum manual raw points:
                 * COA    = 5
                 * Terms  = 10
                 * Others = 5
                 *
                 * Total raw = 20
                 *
                 * Reliability criterion = 10%.
                 */
                var manualPoints =
                    line.CoaPoints +
                    line.TermsPoints +
                    line.OtherPoints;

                line.ReliabilityScore =
                    Math.Round(
                        manualPoints / 20m * 100m,
                        2,
                        MidpointRounding.AwayFromZero);

                line.ReliabilityGrade =
                    Math.Round(
                        manualPoints / 20m * 10m,
                        2,
                        MidpointRounding.AwayFromZero);

                line.TotalGrade =
                    Math.Round(
                        line.QualityGrade +
                        line.DeliveryGrade +
                        line.CostGrade +
                        line.ReliabilityGrade,
                        2,
                        MidpointRounding.AwayFromZero);

                line.Remarks =
                    lineRequest.Remarks;

                line.UpdatedBy =
                    request.UpdatedBy;

                line.UpdatedAt =
                    now;
            }

            SupplierEvaluationGenerationService
                .RecalculateHeader(evaluation);

            evaluation.UpdatedBy =
                request.UpdatedBy;

            evaluation.UpdatedAt =
                now;

            evaluation.WorkflowHistory.Add(
                new SupplierEvaluationWorkflowHistory
                {
                    EvaluationId =
                        evaluation.EvaluationId,

                    FromStatus =
                        evaluation.Status,

                    ToStatus =
                        evaluation.Status,

                    Action =
                        "PURCHASING_UPDATED",

                    ActionBy =
                        request.UpdatedBy,

                    ActionAt =
                        now,

                    Remarks =
                        request.Remarks
                });

            await _context.SaveChangesAsync();

            return CreateSuccessResult(
                evaluation,
                "Supplier evaluation was updated successfully.");
        }

        public async Task<SupplierEvaluationResultDto>
            FinalizeAsync(
                int evaluationId,
                SupplierEvaluationWorkflowActionDto request)
        {
            if (evaluationId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationId));
            }

            if (string.IsNullOrWhiteSpace(
                request.ActionBy))
            {
                throw new ArgumentException(
                    "ActionBy is required.",
                    nameof(request));
            }

            var evaluation = await _context
                .SupplierPerformanceEvaluations
                .Include(x => x.Lines)
                .Include(x => x.WorkflowHistory)
                .FirstOrDefaultAsync(x =>
                    x.EvaluationId ==
                    evaluationId);

            if (evaluation == null)
            {
                return CreateFailedResult(
                    "Supplier evaluation was not found.");
            }

            if (!string.Equals(
                evaluation.Status,
                "PENDING_PURCHASING",
                StringComparison.OrdinalIgnoreCase))
            {
                return CreateFailedResult(
                    "Only PENDING_PURCHASING evaluations can be finalized.",
                    evaluation);
            }

            SupplierEvaluationGenerationService
                .RecalculateHeader(evaluation);

            var now = DateTime.UtcNow;
            var oldStatus = evaluation.Status;

            evaluation.Status =
                "FINALIZED";

            evaluation.FinalizedBy =
                request.ActionBy;

            evaluation.FinalizedAt =
                now;

            evaluation.UpdatedBy =
                request.ActionBy;

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
                        "FINALIZED",

                    Action =
                        "FINALIZED",

                    ActionBy =
                        request.ActionBy,

                    ActionAt =
                        now,

                    Remarks =
                        request.Remarks
                });

            await _context.SaveChangesAsync();

            return CreateSuccessResult(
                evaluation,
                "Supplier evaluation was finalized successfully.");
        }

        public async Task<SupplierEvaluationMonthlySummaryDto>
            GetMonthlySummaryAsync(
                int year,
                int month)
        {
            if (year < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year));
            }

            if (month is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month));
            }

            var start =
                new DateTime(
                    year,
                    month,
                    1);

            var end =
                start.AddMonths(1);

            var evaluations = await _context
                .SupplierPerformanceEvaluations
                .AsNoTracking()
                .Where(x =>
                    x.EvaluationDate.HasValue &&
                    x.EvaluationDate.Value >= start &&
                    x.EvaluationDate.Value < end)
                .ToListAsync();

            var total =
                evaluations.Count;

            return new SupplierEvaluationMonthlySummaryDto
            {
                EvaluationYear =
                    year,

                EvaluationMonth =
                    month,

                EvaluationMonthName =
                    start.ToString("MMMM"),

                TotalSuppliers =
                    evaluations
                        .Select(x => x.SupplierId)
                        .Distinct()
                        .Count(),

                TotalEvaluations =
                    total,

                GeneratedCount =
                    evaluations.Count(x =>
                        x.Status ==
                        "PENDING_PURCHASING"),

                FinalizedCount =
                    evaluations.Count(x =>
                        x.Status ==
                        "FINALIZED"),

                AverageQualityScore =
                    total == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.QualityScore),
                            2),

                AverageDeliveryScore =
                    total == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.OnTimeDeliveryScore),
                            2),

                AverageCostScore =
                    total == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.CostCompetitivenessScore),
                            2),

                AverageReliabilityScore =
                    total == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.ReliabilityScore),
                            2),

                AverageTotalScore =
                    total == 0
                        ? 0m
                        : Math.Round(
                            evaluations.Average(x =>
                                x.TotalScore),
                            2),

                ExcellentCount =
                    evaluations.Count(x =>
                        x.PerformanceRating ==
                        "EXCELLENT"),

                VeryGoodCount =
                    evaluations.Count(x =>
                        x.PerformanceRating ==
                        "VERY GOOD"),

                GoodCount =
                    evaluations.Count(x =>
                        x.PerformanceRating ==
                        "GOOD"),

                NeedsImprovementCount =
                    evaluations.Count(x =>
                        x.PerformanceRating ==
                        "NEEDS IMPROVEMENT"),

                PoorCount =
                    evaluations.Count(x =>
                        x.PerformanceRating ==
                        "POOR")
            };
        }

        private static void ValidateReliabilityPoints(
            SaveSupplierEvaluationReliabilityLineDto line)
        {
            if (line.CoaPoints < 0m ||
                line.CoaPoints > 5m)
            {
                throw new InvalidOperationException(
                    "COA points must be between 0 and 5.");
            }

            if (line.TermsPoints < 0m ||
                line.TermsPoints > 10m)
            {
                throw new InvalidOperationException(
                    "Terms points must be between 0 and 10.");
            }

            if (line.OtherPoints < 0m ||
                line.OtherPoints > 5m)
            {
                throw new InvalidOperationException(
                    "Other points must be between 0 and 5.");
            }
        }

        private static SupplierEvaluationResultDto
            CreateSuccessResult(
                SupplierPerformanceEvaluation evaluation,
                string message)
        {
            return new SupplierEvaluationResultDto
            {
                Success = true,
                Message = message,
                EvaluationId =
                    evaluation.EvaluationId,
                EvaluationNo =
                    evaluation.EvaluationNo,
                Status =
                    evaluation.Status
            };
        }

        private static SupplierEvaluationResultDto
            CreateFailedResult(
                string message,
                SupplierPerformanceEvaluation? evaluation = null)
        {
            return new SupplierEvaluationResultDto
            {
                Success = false,
                Message = message,
                EvaluationId =
                    evaluation?.EvaluationId,
                EvaluationNo =
                    evaluation?.EvaluationNo,
                Status =
                    evaluation?.Status
            };
        }
    }
}