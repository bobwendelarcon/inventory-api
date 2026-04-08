namespace inventory_api.Models
{
    public class ProductLotNumber
    {
        public long lot_entry_id { get; set; }
        public string product_id { get; set; } = string.Empty;
        public string branch_id { get; set; } = string.Empty;
        public string lot_no { get; set; } = string.Empty;
        public decimal quantity { get; set; }
        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}