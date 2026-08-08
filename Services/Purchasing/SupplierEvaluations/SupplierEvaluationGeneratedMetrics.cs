namespace inventory_api.Services.Purchasing.SupplierEvaluations
{
    public class SupplierEvaluationGeneratedMetrics
    {
        public int SupplierId { get; set; }

        public int EvaluationYear { get; set; }

        public int EvaluationMonth { get; set; }

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public GeneratedQualityMetric Quality { get; set; }
            = new();

        public GeneratedDeliveryMetric Delivery { get; set; }
            = new();

        public GeneratedCostMetric Cost { get; set; }
            = new();
    }

    public class GeneratedQualityMetric
    {
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

    public class GeneratedDeliveryMetric
    {
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

    public class GeneratedCostMetric
    {
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
}