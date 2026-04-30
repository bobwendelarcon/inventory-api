namespace inventory_api.DTOs.Reports
{
    public class InventoryReportRowDto
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string BranchId { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string LotNo { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = "";
        public decimal? PackQty { get; set; }
        public string? PackUom { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public int? MonthsLeft { get; set; }
        public string ExpiryStatus { get; set; } = "";
        public string StockStatus { get; set; } = "";
    }

    public class InventoryReportDto
    {
        public decimal TotalStockQty { get; set; }
        public int TotalLots { get; set; }
        public int ExpiredLots { get; set; }
        public int NearExpiryLots { get; set; }
        public List<InventoryReportRowDto> Items { get; set; } = new();
    }
}