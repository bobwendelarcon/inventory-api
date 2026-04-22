namespace inventory_api.DTOs
{
    public class DeliveryChecklistListDto
    {
        public long checklist_id { get; set; }
        public string checklist_no { get; set; } = string.Empty;
        public DateTime? delivery_date { get; set; }
        public string? route_name { get; set; }
        public string? truck_name { get; set; }
        public string? driver_name { get; set; }
        public int total_customers { get; set; }
        public int total_lines { get; set; }
        public string status { get; set; } = string.Empty;
        public DateTime? created_at { get; set; }
        public string? lot_no { get; set; }
        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }
    }
}