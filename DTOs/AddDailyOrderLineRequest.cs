namespace inventory_api.DTOs
{
    public class AddDailyOrderLineRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
    }
}