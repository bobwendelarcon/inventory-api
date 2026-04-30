namespace inventory_api.DTOs.Reports
{
    public class ReportFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public string? BranchId { get; set; }
        public string? ProductId { get; set; }
        public string? CustomerId { get; set; }
        public string? RouteName { get; set; }
        public string? Region { get; set; }

        public string? ExpiryStatus { get; set; } // EXPIRED, NEAR, SAFE
        public string? StockStatus { get; set; } // WITH_STOCK, ZERO, LOW
        public int? MonthsLeft { get; set; }
    }
}