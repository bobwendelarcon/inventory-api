namespace inventory_api.Models
{
    public class Partner
    {
        public string partner_id { get; set; } = string.Empty;
        public string partner_name { get; set; } = string.Empty;
        public string? address { get; set; }
        public string? contact{ get; set; }
        public string? partner_type { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}