namespace inventory_api.DTOs.Reports
{
    public class DeliveryKpiSummaryDto
    {
        public int TotalDeliveries { get; set; }
        public decimal OnTimePercent { get; set; }
        public decimal DelayedPercent { get; set; }
        public decimal AverageDeliveryDays { get; set; }
    }

    public class DeliveryKpiRowDto
    {
        public string OrderNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string RouteName { get; set; } = "";
        public string Region { get; set; } = "";
        public DateTime? DateOrdered { get; set; }
        public DateTime? DateDelivered { get; set; }
        public int DeliveryDays { get; set; }
        public int TargetDays { get; set; }
        public string KpiStatus { get; set; } = "";
        public string TransactionReference { get; set; } = "";
    }

    public class DeliveryKpiReportDto
    {
        public DeliveryKpiSummaryDto Summary { get; set; } = new();
        public List<DeliveryKpiRowDto> Items { get; set; } = new();
    }
}