using System.ComponentModel.DataAnnotations;

namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

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

public class SupplierEvaluationListDto
{
    public int EvaluationId { get; set; }

    public string EvaluationNo { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;


    // Delivery references

    public int? PoId { get; set; }

    public string PoNo { get; set; } = string.Empty;

    public int? RrId { get; set; }

    public string RrNo { get; set; } = string.Empty;

    public int? QcId { get; set; }

    public string QcNo { get; set; } = string.Empty;


    // Evaluation dates

    public DateTime? EvaluationDate { get; set; }

    public DateTime? DeliveryDate { get; set; }


    // Summary scores

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

public class SupplierEvaluationDetailsDto
{
    public int EvaluationId { get; set; }

    public string EvaluationNo { get; set; } = string.Empty;


    // Supplier

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string? SupplierType { get; set; }

    public string? ContactPerson { get; set; }


    // Purchasing references

    public int? PoId { get; set; }

    public string PoNo { get; set; } = string.Empty;

    public int? ScheduleId { get; set; }

    public int? RrId { get; set; }

    public string RrNo { get; set; } = string.Empty;

    public int? QcId { get; set; }

    public string QcNo { get; set; } = string.Empty;


    // Dates

    public DateTime? EvaluationDate { get; set; }

    public DateTime? DeliveryDate { get; set; }


    // Header scores

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


    // Workflow

    public string Status { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public string? GeneratedBy { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public string? FinalizedBy { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }


    // Per-material evaluation

    public List<SupplierEvaluationLineDto> Lines { get; set; }
        = new();


    public List<SupplierEvaluationWorkflowHistoryDto> WorkflowHistory { get; set; }
        = new();
}