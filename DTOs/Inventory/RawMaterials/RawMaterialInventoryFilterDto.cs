namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialInventoryFilterDto
    {
        public string? Search { get; set; }

        public string? BranchId { get; set; }

        public int? CategoryId { get; set; }

        public string? StockStatus { get; set; }

        public string? ExpiryStatus { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}