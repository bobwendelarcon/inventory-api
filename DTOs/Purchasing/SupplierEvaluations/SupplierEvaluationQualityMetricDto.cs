namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SupplierEvaluationQualityMetricDto
{
    public int QualityMetricId { get; set; }

    public int EvaluationId { get; set; }

    public int ReceivingReportCount { get; set; }

    public int QcInspectionCount { get; set; }

    public decimal TotalReceivedQty { get; set; }

    public decimal TotalAcceptedQty { get; set; }

    public decimal TotalRejectedQty { get; set; }

    public decimal AcceptanceRate { get; set; }

    public decimal RejectionRate { get; set; }

    public decimal QualityScore { get; set; }

    public string? CalculationRemarks { get; set; }
}

public class SupplierEvaluationDeliveryMetricDto
{
    public int DeliveryMetricId { get; set; }

    public int EvaluationId { get; set; }

    public int ScheduledDeliveries { get; set; }

    public int CompletedDeliveries { get; set; }

    public int OnTimeDeliveries { get; set; }

    public int LateDeliveries { get; set; }

    public int EarlyDeliveries { get; set; }

    public int IncompleteDeliveries { get; set; }

    public int UndeliveredSchedules { get; set; }

    public decimal OnTimeDeliveryRate { get; set; }

    public decimal AverageDelayDays { get; set; }

    public decimal DeliveryScore { get; set; }

    public string? CalculationRemarks { get; set; }
}

public class SupplierEvaluationCostMetricDto
{
    public int CostMetricId { get; set; }

    public int EvaluationId { get; set; }

    public int PurchaseOrderCount { get; set; }

    public int PurchaseOrderLineCount { get; set; }

    public decimal TotalPurchaseAmount { get; set; }

    public decimal SupplierAverageUnitPrice { get; set; }

    public decimal ComparisonAverageUnitPrice { get; set; }

    public decimal PriceVarianceAmount { get; set; }

    public decimal PriceVariancePercentage { get; set; }

    public decimal CostScore { get; set; }

    public string? CalculationRemarks { get; set; }
}

public class SupplierEvaluationReliabilityDto
{
    public int ReliabilityScoreId { get; set; }

    public int EvaluationId { get; set; }

    public decimal ResponsivenessScore { get; set; }

    public decimal IssueResolutionScore { get; set; }

    public decimal ReplacementSupportScore { get; set; }

    public decimal CommunicationScore { get; set; }

    public decimal ReliabilityScore { get; set; }

    public string? Remarks { get; set; }

    public string? ScoredBy { get; set; }

    public DateTime? ScoredAt { get; set; }
}