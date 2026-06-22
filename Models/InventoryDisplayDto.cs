namespace inventory_api.DTOs
{
    public class InventoryDisplayDto
    {
        public string product_id { get; set; } = "";
        public string branch_id { get; set; } = "";
        public string description { get; set; } = "";
        public string? product_description { get; set; }
        public string category_name { get; set; } = "";
        public string uom { get; set; } = "";
        public decimal pack_qty { get; set; }
        public string pack_uom { get; set; } = "";

        public decimal stock_level { get; set; }

        public string lot_no { get; set; } = "";
        public string warehouse { get; set; } = "";
        public decimal qty { get; set; }
        public string date { get; set; } = "";
        public string manufacturing_date { get; set; } = "";
        public string expiration_date { get; set; } = "";

        public decimal reserved_qty { get; set; }
        public decimal available_qty { get; set; }
        public List<InventoryReservedDetailDto> reserved_details { get; set; } = new();

        //  public List<InventoryReservedDetailDto> reserved_details { get; set; } = new();
    }
}
