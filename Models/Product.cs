namespace inventory_api.Models
{
    public class Product
    {
        public string product_id { get; set; } = string.Empty;
        public string? product_sku { get; set; }
        public string product_name { get; set; } = string.Empty;
        public string? product_description { get; set; }
        public decimal product_price { get; set; }
        public string? uom { get; set; }
        public string? pack_uom { get; set; }     // ✅ added
        public decimal? pack_qty { get; set; }    // ✅ added
        public decimal stock_level { get; set; }
        public string? catg_id { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}