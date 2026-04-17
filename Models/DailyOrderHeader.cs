namespace inventory_api.Models
{
    public class DailyOrderHeader
    {
        public long order_id { get; set; }
        public string order_no { get; set; } = string.Empty;

        public string customer_name { get; set; } = string.Empty;
        public string? class_name { get; set; }
        public string? route_name { get; set; }

        public DateTime? date_ordered { get; set; }
        public DateTime? delivery_date { get; set; }
        public DateTime? date_delivered { get; set; }

        public string status { get; set; } = "Draft";
        public string? special_instructions { get; set; }

        public string? created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool is_deleted { get; set; } = false;
        public DateTime? deleted_at { get; set; }
        public string? deleted_by { get; set; }

        public ICollection<DailyOrderLine> Lines { get; set; } = new List<DailyOrderLine>();
    }
}