namespace inventory_api.DTOs
{
    public class UpdateDailyOrderRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public string? RouteName { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}