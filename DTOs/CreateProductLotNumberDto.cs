namespace inventory_api.DTOs
{
    public class CreateProductLotNumberDto
    {
        public string lot_no { get; set; } = string.Empty;
        public string product_id { get; set; } = string.Empty;
        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }
        public decimal quantity { get; set; }
        public string branch_id { get; set; } = string.Empty;
    }
}