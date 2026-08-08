namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SupplierEvaluationMonthlySummaryDto
{
    public int EvaluationYear { get; set; }

    public int EvaluationMonth { get; set; }

    public string EvaluationMonthName { get; set; } =
        string.Empty;

    public int TotalSuppliers { get; set; }

    public int TotalEvaluations { get; set; }

    public int GeneratedCount { get; set; }

    public int FinalizedCount { get; set; }

    public decimal AverageQualityScore { get; set; }

    public decimal AverageDeliveryScore { get; set; }

    public decimal AverageCostScore { get; set; }

    public decimal AverageReliabilityScore { get; set; }

    public decimal AverageTotalScore { get; set; }

    public int ExcellentCount { get; set; }

    public int VeryGoodCount { get; set; }

    public int GoodCount { get; set; }

    public int NeedsImprovementCount { get; set; }

    public int PoorCount { get; set; }

    public List<SupplierEvaluationListDto> Evaluations { get; set; } =
        new();
}