using System.ComponentModel.DataAnnotations;

namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SupplierEvaluationWorkflowActionDto
{
    [Required]
    [MaxLength(50)]
    public string ActionBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

public class SubmitSupplierEvaluationDto
{
    [Required]
    [MaxLength(50)]
    public string SubmittedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

public class ReviewSupplierEvaluationDto
{
    [Required]
    [MaxLength(50)]
    public string ReviewedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

public class ApproveSupplierEvaluationDto
{
    [Required]
    [MaxLength(50)]
    public string ApprovedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

public class FinalizeSupplierEvaluationDto
{
    [Required]
    [MaxLength(50)]
    public string FinalizedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

public class ReturnSupplierEvaluationDto
{
    [Required]
    [MaxLength(50)]
    public string ReturnedBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Remarks { get; set; } = string.Empty;
}

public class SupplierEvaluationWorkflowHistoryDto
{
    public long HistoryId { get; set; }

    public int EvaluationId { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public string ActionBy { get; set; } = string.Empty;

    public DateTime ActionAt { get; set; }
}