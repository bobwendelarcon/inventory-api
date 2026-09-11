namespace inventory_api.DTOs.Reports.RawMaterials
{
    public class RawMaterialUsageTrendFilterDto
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? BranchId { get; set; }

        public int? MaterialId { get; set; }

        public int? CategoryId { get; set; }

        public string? Search { get; set; }
    }


    public class RawMaterialUsageTrendResponseDto
    {
        public RawMaterialUsageTrendSummaryDto Summary { get; set; }
            = new();

        public List<RawMaterialUsageTrendDailyDto> DailyTrend { get; set; }
            = new();

        public List<RawMaterialUsageTrendMaterialDto> Materials { get; set; }
            = new();
    }


    public class RawMaterialUsageTrendSummaryDto
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int TotalDays { get; set; }

        public int ReleaseTransactions { get; set; }

        public int UniqueMaterials { get; set; }

        public int ActiveUsageDays { get; set; }
    }


    public class RawMaterialUsageTrendDailyDto
    {
        public DateTime Date { get; set; }

        public List<RawMaterialUsageTrendByUomDto> UsageByUom { get; set; }
            = new();

        public int TransactionCount { get; set; }
    }


    public class RawMaterialUsageTrendByUomDto
    {
        public string Uom { get; set; } = "";

        public decimal Quantity { get; set; }
    }


    public class RawMaterialUsageTrendMaterialDto
    {
        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = "";

        public string MaterialName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public string Uom { get; set; } = "";

        public decimal TotalUsage { get; set; }

        public decimal AverageDailyUsage { get; set; }

        public decimal AverageUsagePerActiveDay { get; set; }

        public int ActiveUsageDays { get; set; }

        public int ReleaseTransactions { get; set; }

        public DateTime? FirstUsageDate { get; set; }

        public DateTime? LastUsageDate { get; set; }
    }
}