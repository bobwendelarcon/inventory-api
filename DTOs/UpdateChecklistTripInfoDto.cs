namespace inventory_api.DTOs
{
    public class UpdateChecklistTripInfoDto
    {
        public long checklist_id { get; set; }

        public string route_name { get; set; }
            = string.Empty;

        public string truck_name { get; set; }
            = string.Empty;

        public string driver_name { get; set; }
            = string.Empty;
    }
}