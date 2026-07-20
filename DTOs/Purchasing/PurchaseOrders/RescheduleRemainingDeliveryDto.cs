namespace inventory_api.DTOs.Purchasing.PurchaseOrders
{
    public class RescheduleRemainingDeliveryDto
    {
        public string? Reason { get; set; }

        public string? CreatedBy { get; set; }

        public List<RescheduleDeliveryScheduleDto> Schedules { get; set; } = new();
    }

    public class RescheduleDeliveryScheduleDto
    {
        public DateTime ScheduledDate { get; set; }

        public string? Remarks { get; set; }

        public List<RescheduleRemainingDeliveryLineDto> Lines { get; set; } = new();
    }

    public class RescheduleRemainingDeliveryLineDto
    {
        public int ScheduleLineId { get; set; }

        public decimal RescheduleQty { get; set; }
    }
}