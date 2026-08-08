using System.ComponentModel.DataAnnotations;

namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SaveSupplierReliabilityScoreDto
{
    [Range(0, 100)]
    public decimal ResponsivenessScore { get; set; }

    [Range(0, 100)]
    public decimal IssueResolutionScore { get; set; }

    [Range(0, 100)]
    public decimal ReplacementSupportScore { get; set; }

    [Range(0, 100)]
    public decimal CommunicationScore { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    [MaxLength(50)]
    public string ScoredBy { get; set; } = string.Empty;
}