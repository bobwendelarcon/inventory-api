namespace inventory_api.DTOs.Reports.RawMaterials
{
    public class RawMaterialForecastFilterDto
    {
        public string? BranchId { get; set; }
        public int? MaterialId { get; set; }
        public int? CategoryId { get; set; }
        public string? Search { get; set; }

        public int UsageHistoryDays { get; set; } = 30;
        public int SafetyStockDays { get; set; } = 14;
        public int TargetCoverDays { get; set; } = 60;

        public string? Status { get; set; }
    }

    public class RawMaterialForecastResponseDto
    {
        public RawMaterialForecastSummaryDto Summary { get; set; } = new();
        public List<RawMaterialForecastItemDto> Items { get; set; } = new();
    }

    public class RawMaterialForecastSummaryDto
    {
        public int TotalMaterials { get; set; }
        public int Healthy { get; set; }
        public int Watch { get; set; }
        public int Reorder { get; set; }
        public int Critical { get; set; }
        public int NoUsageHistory { get; set; }
    }

    public class RawMaterialForecastItemDto
    {
        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string Uom { get; set; } = "";

        public decimal CurrentStock { get; set; }
        public decimal IncomingPoQty { get; set; }
        public decimal StockPosition { get; set; }

        public decimal UsageDuringHistory { get; set; }
        public decimal AverageDailyUsage { get; set; }

        public int? LeadTimeDays { get; set; }
        public decimal LeadTimeDemand { get; set; }

        public int SafetyStockDays { get; set; }
        public decimal SafetyStockQty { get; set; }

        public decimal ReorderPoint { get; set; }

        public decimal? DaysCover { get; set; }
        public decimal? DaysCoverWithIncoming { get; set; }

        public int TargetCoverDays { get; set; }
        public decimal TargetStockQty { get; set; }

        public decimal SuggestedOrderQty { get; set; }

        public DateTime? NextExpectedDelivery { get; set; }

        public string SupplierName { get; set; } = "";

        public string Status { get; set; } = "";
    }
}