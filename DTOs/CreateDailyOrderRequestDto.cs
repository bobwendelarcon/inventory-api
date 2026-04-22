namespace inventory_api.DTOs
{
    public class CreateDailyOrderRequest
    {
        public string OrderNo { get; set; } = string.Empty;
        public string? CustomerId { get; set; }      // ✅ NEW
        public string CustomerName { get; set; } = string.Empty;

        public string? SourceBranchId { get; set; }  // ✅ NEW
        public string? ClassName { get; set; }
        public string? RouteName { get; set; }
        public DateTime? DateOrdered { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? SpecialInstructions { get; set; }
        public string? CreatedBy { get; set; }
        public List<CreateDailyOrderLineRequest> Lines { get; set; } = new();
    }

    public class CreateDailyOrderLineRequest
    {
        public string? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
    }

}
