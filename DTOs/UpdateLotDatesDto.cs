namespace inventory_api.DTOs
{
    public class UpdateLotDatesDto
    {
        public string product_id { get; set; } = "";
        public string branch_id { get; set; } = "";
        public string lot_no { get; set; } = "";
        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }
    }
}