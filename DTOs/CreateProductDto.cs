namespace inventory_api.DTOs
{
    public class CreateProductDto
    {
        public string product_id { get; set; } = string.Empty;
        public string product_sku { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public string product_description { get; set; } = string.Empty;
        public double product_price { get; set; }
        public string uom { get; set; } = string.Empty;
        public double stock_level { get; set; }
        public string catg_id { get; set; } = string.Empty;
        
       
    }
}
