namespace inventory_api.DTOs
{
    public class CreateProductLotNumberDto
    {
        public string lot_no { get; set; } = string.Empty;
        public string product_id { get; set; } = string.Empty;
        public string manufacturing_date { get; set; } = string.Empty;
        public string expiration_date { get; set; } = string.Empty; // IN / OUT
        public double quantity { get; set; }
        public string branch_id { get; set; } = string.Empty;

    }
}