namespace inventory_api.DTOs
{
    public class DashboardChecklistDto
    {
        public long ChecklistId { get; set; }
        public string ChecklistNo { get; set; } = "";
        public DateTime? DeliveryDate { get; set; }

        public string TruckName { get; set; } = "";
        public string DriverName { get; set; } = "";

        public string Status { get; set; } = "";
        // READY / LOADING / PARTIAL / COMPLETED
    }
}