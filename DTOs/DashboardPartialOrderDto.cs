namespace inventory_api.DTOs
{
    public class DashboardPartialOrderDto
    {
        public long OrderId { get; set; }
        public string OrderNo { get; set; } = "";

        public string CustomerName { get; set; } = "";

        public decimal RemainingQty { get; set; }

        public string Status { get; set; } = "";
        // PARTIAL / PARTIALLY DELIVERED
    }
}