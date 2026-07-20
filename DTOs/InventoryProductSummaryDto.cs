namespace inventory_api.DTOs
{
    public class InventoryProductSummaryDto
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ProductDescription { get; set; } = "";
        public string CategoryName { get; set; } = "";

        public decimal TotalQty { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal AvailableQty { get; set; }

        public decimal StockLevel { get; set; }
        public decimal DeficitQty { get; set; }

        public string Uom { get; set; } = "";
        public decimal PackQty { get; set; }
        public string PackUom { get; set; } = "";
        public string PackDisplay { get; set; } = "";

        public string StockStatus { get; set; } = "";
    }
}