namespace inventory_api.DTOs
{
    public class CreateProductDto
    {
        public string product_id { get; set; } = string.Empty;
        public string? product_sku { get; set; }
        public string product_name { get; set; } = string.Empty;
        public string? product_description { get; set; }
        public decimal product_price { get; set; }
        public string? uom { get; set; }

        // ✅ NEW (pack support)
        public string? pack_uom { get; set; }
        public decimal? pack_qty { get; set; }
        public bool is_deleted { get; set; }
        public decimal stock_level { get; set; }
        public string? catg_id { get; set; }
    }
}