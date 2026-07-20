namespace inventory_api.Models.Purchasing.PurchaseOrders
{
    public class PurchaseOrderDeliveryScheduleLine
    {
        public int ScheduleLineId { get; set; }

        public int ScheduleId { get; set; }

        public int PoLineId { get; set; }

        public decimal ScheduledQty { get; set; }

        public decimal ReceivedQty { get; set; }

        public decimal BalanceQty { get; set; }

        public string Status { get; set; } = "OPEN";

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public PurchaseOrderDeliverySchedule? Schedule { get; set; }

        public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    }
}