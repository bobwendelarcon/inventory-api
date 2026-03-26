namespace inventory_api.DTOs
{
    public class InventoryDisplayDto
    {
        public string product_id { get; set; }
        public string description { get; set; }
        public string uom { get; set; }
        public string lot_no { get; set; }
        public string warehouse { get; set; }
        public int qty { get; set; }
        public string date { get; set; }

        // ✅ ADD THESE
        public string manufacturing_date { get; set; } = string.Empty;
        public string expiration_date { get; set; } = string.Empty;
    }
}
