namespace inventory_api.DTOs
{
    public class ChecklistListItemDto
    {
        public long checklist_id { get; set; }
        public string checklist_no { get; set; } = string.Empty;
        public string? route_name { get; set; }
        public string? truck_name { get; set; }
        public string? driver_name { get; set; }
        public string status { get; set; } = string.Empty;
        public string created_at { get; set; } = string.Empty;
    }
}