namespace inventory_api.Models.Purchasing.PurchaseOrders
{
    public class PurchaseOrderDeliverySchedule
    {
        public int ScheduleId { get; set; }

        public int PoId { get; set; }

        public int ScheduleNo { get; set; }

        public DateTime ScheduledDate { get; set; }

        public string Status { get; set; } = "OPEN";

        public int? RescheduledFromScheduleId { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public PurchaseOrderHeader? PurchaseOrder { get; set; }

        public PurchaseOrderDeliverySchedule? RescheduledFromSchedule { get; set; }

        public List<PurchaseOrderDeliveryScheduleLine> Lines { get; set; } = new();
    }
}