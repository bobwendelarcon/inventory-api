namespace inventory_api.DTOs
{
    public class AllocateDailyOrderRequest
    {
        public List<AllocateDailyOrderLineRequest> Lines { get; set; } = new();
    }

    public class AllocateDailyOrderLineRequest
    {
        public long OrderLineId { get; set; }

        // user entered qty
        public decimal AllocateQty { get; set; }
    }
}