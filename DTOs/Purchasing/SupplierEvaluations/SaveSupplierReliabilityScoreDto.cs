using System.ComponentModel.DataAnnotations;

namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SaveSupplierEvaluationReliabilityDto
{
    [Required]
    [MaxLength(50)]
    public string UpdatedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    [MinLength(1)]
    public List<SaveSupplierEvaluationReliabilityLineDto> Lines { get; set; }
        = new();
}

public class SaveSupplierEvaluationReliabilityLineDto
{
    [Range(1, int.MaxValue)]
    public int EvaluationLineId { get; set; }

    // Documents / COA = maximum 5 points.
    [Range(0, 5)]
    public decimal CoaPoints { get; set; }

    // Terms = maximum 10 points.
    [Range(0, 10)]
    public decimal TermsPoints { get; set; }

    // Others = maximum 5 points.
    [Range(0, 5)]
    public decimal OtherPoints { get; set; }

    [MaxLength(2000)]
    public string? Remarks { get; set; }
}