namespace inventory_api.Models
{
    public class DeliveryChecklistHeader
    {
        public long checklist_id { get; set; }
        public string checklist_no { get; set; } = string.Empty;

        public DateTime? delivery_date { get; set; }
        public string? route_name { get; set; }
        public string? truck_name { get; set; }
        public string? driver_name { get; set; }
        public string? helper_name { get; set; }

        public string status { get; set; } = "READY";
        public string? remarks { get; set; }
        public string? created_by { get; set; }
        public DateTime created_at { get; set; }

        public List<DeliveryChecklistLine> Lines { get; set; } = new();
    }
}