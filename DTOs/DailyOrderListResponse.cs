namespace inventory_api.DTOs
{
    public class DailyOrderListResponse
    {
        public DailyOrderSummaryDto Summary { get; set; } = new();
        public List<DailyOrderListDto> Data { get; set; } = new();
    }

    public class DailyOrderSummaryDto
    {
        public int TotalOrders { get; set; }
        public int ForAllocation { get; set; }
        public int Allocated { get; set; }
        public int Partial { get; set; }
        public int ReadyDispatch { get; set; }
        public int Overdue { get; set; }
        public int Completed { get; set; }
        public int PartiallyDelivered { get; set; }
    }
}