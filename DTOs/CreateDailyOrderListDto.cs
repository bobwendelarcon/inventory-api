namespace inventory_api.DTOs
{
    public class DailyOrderListDto
    {
        public long OrderId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Month { get; set; } = string.Empty;
        public string OrderNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal RemainingQty { get; set; }
        public string AllocationStatus { get; set; } = string.Empty;
        public DateTime? DateOrdered { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime? DateDelivered { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SpecialInstructions { get; set; }
    }
}
