using System.Collections.Generic;

namespace inventory_api.DTOs
{
    public class ChecklistDetailsDto
    {
        public long checklist_id { get; set; }
        public string checklist_no { get; set; } = string.Empty;
        public DateTime? delivery_date { get; set; }
        public string? route_name { get; set; }
        public string? truck_name { get; set; }
        public string? driver_name { get; set; }
        public string? helper_name { get; set; }
        public string status { get; set; } = string.Empty;
        public string? remarks { get; set; }

        public List<ChecklistDetailLineDto> lines { get; set; } = new();
    }
}