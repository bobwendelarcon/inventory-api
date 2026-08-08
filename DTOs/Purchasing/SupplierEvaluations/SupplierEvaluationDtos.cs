using System.ComponentModel.DataAnnotations;

namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

/// <summary>
/// Request used to generate or regenerate a monthly supplier evaluation.
/// </summary>
public class GenerateSupplierEvaluationDto
{
    [Required]
    public int SupplierId { get; set; }

    [Range(2000, 9999)]
    public int EvaluationYear { get; set; }

    [Range(0, 100)]
    public decimal ReliabilityScore { get; set; }

    [MaxLength(1000)]
    public string? ReliabilityRemarks { get; set; }

    [Range(1, 12)]
    public int EvaluationMonth { get; set; }

    [Required]
    [MaxLength(50)]
    public string GeneratedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

/// <summary>
/// Request used to generate evaluations for all eligible suppliers in a month.
/// </summary>
public class GenerateMonthlySupplierEvaluationsDto
{
    [Range(2000, 9999)]
    public int EvaluationYear { get; set; }

    [Range(1, 12)]
    public int EvaluationMonth { get; set; }

    [Required]
    [MaxLength(50)]
    public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Query filters for the supplier evaluation list.
/// </summary>
public class SupplierEvaluationFilterDto
{
    public int? SupplierId { get; set; }

    [Range(2000, 9999)]
    public int? EvaluationYear { get; set; }

    [Range(1, 12)]
    public int? EvaluationMonth { get; set; }

    [MaxLength(40)]
    public string? Status { get; set; }

    [MaxLength(100)]
    public string? Search { get; set; }
}

/// <summary>
/// Data displayed in the main supplier evaluation grid.
/// </summary>
public class SupplierEvaluationListDto
{
    public int EvaluationId { get; set; }

    public string EvaluationNo { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public int EvaluationYear { get; set; }

    public int EvaluationMonth { get; set; }

    public string EvaluationMonthName { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal QualityScore { get; set; }

    public decimal OnTimeDeliveryScore { get; set; }

    public decimal CostCompetitivenessScore { get; set; }

    public decimal ReliabilityScore { get; set; }

    public decimal TotalScore { get; set; }

    public string PerformanceRating { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? GeneratedBy { get; set; }
}

/// <summary>
/// Complete evaluation details displayed by View Details.
/// </summary>
public class SupplierEvaluationDetailsDto
{
    public int EvaluationId { get; set; }

    public string EvaluationNo { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string? SupplierType { get; set; }

    public string? ContactPerson { get; set; }

    public int EvaluationYear { get; set; }

    public int EvaluationMonth { get; set; }

    public string EvaluationMonthName { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal QualityScore { get; set; }

    public decimal QualityWeightedScore { get; set; }

    public decimal OnTimeDeliveryScore { get; set; }

    public decimal DeliveryWeightedScore { get; set; }

    public decimal CostCompetitivenessScore { get; set; }

    public decimal CostWeightedScore { get; set; }

    public decimal ReliabilityScore { get; set; }

    public decimal ReliabilityWeightedScore { get; set; }

    public decimal TotalScore { get; set; }

    public string PerformanceRating { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public string? GeneratedBy { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? FinalizedBy { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public SupplierEvaluationQualityMetricDto? QualityMetric { get; set; }

    public SupplierEvaluationDeliveryMetricDto? DeliveryMetric { get; set; }

    public SupplierEvaluationCostMetricDto? CostMetric { get; set; }

    public SupplierEvaluationReliabilityDto? ReliabilityAssessment { get; set; }

    public List<SupplierEvaluationWorkflowHistoryDto> WorkflowHistory { get; set; }
        = new();
}