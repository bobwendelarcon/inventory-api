namespace inventory_api.DTOs
{
    public class InventoryPrintSummaryDto
    {
        public string product_id { get; set; } = "";
        public string product_name { get; set; } = "";
        public string product_description { get; set; } = "";
        public string category_name { get; set; } = "";

        public decimal available_qty { get; set; }

        public string uom { get; set; } = "";
        public decimal pack_qty { get; set; }
        public string pack_uom { get; set; } = "";

        public string pack_display { get; set; } = "";
    }
}