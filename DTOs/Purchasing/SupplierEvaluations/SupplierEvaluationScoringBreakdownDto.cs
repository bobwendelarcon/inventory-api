namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SupplierEvaluationScoringBreakdownDto
{
    public int EvaluationId { get; set; }

    public string EvaluationNo { get; set; } = string.Empty;

    public decimal QualityRawScore { get; set; }

    public decimal QualityWeight { get; set; } = 40m;

    public decimal QualityWeightedScore { get; set; }

    public decimal DeliveryRawScore { get; set; }

    public decimal DeliveryWeight { get; set; } = 30m;

    public decimal DeliveryWeightedScore { get; set; }

    public decimal CostRawScore { get; set; }

    public decimal CostWeight { get; set; } = 20m;

    public decimal CostWeightedScore { get; set; }

    public decimal ReliabilityRawScore { get; set; }

    public decimal ReliabilityWeight { get; set; } = 10m;

    public decimal ReliabilityWeightedScore { get; set; }

    public decimal TotalScore { get; set; }

    public string PerformanceRating { get; set; } = string.Empty;
}