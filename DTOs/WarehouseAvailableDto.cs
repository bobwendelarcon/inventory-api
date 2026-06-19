namespace inventory_api.DTOs
{
    public class WarehouseAvailableDto
    {
        public string? BranchId { get; set; }
        public string WarehouseName { get; set; } = "";
        public decimal AvailableQty { get; set; }
        public bool IsPreferred { get; set; }
    }
}
