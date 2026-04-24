namespace inventory_api.DTOs
{
    public class DashboardInventoryAlertDto
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";

        public decimal Quantity { get; set; }        // for LOW STOCK / OUT OF STOCK available qty
        public decimal StockLevel { get; set; }      // product stock level threshold

        public decimal AvailableQty { get; set; }    // for PLANNING SHORTAGE display
        public decimal RequiredQty { get; set; }
        public decimal ShortageQty { get; set; }
        public decimal ReservedQty { get; set; } // NEW

        public string Uom { get; set; } = "";
        public string AlertType { get; set; } = "";
    }
}