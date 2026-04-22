namespace inventory_api.DTOs
{
    public class InventoryAdjustRequestDto
    {
        public string product_id { get; set; } = string.Empty;
        public string lot_no { get; set; } = string.Empty;
        public string branch_id { get; set; } = string.Empty;

        public string adjustment_type { get; set; } = string.Empty; // ADD / DEDUCT / SET
        public decimal quantity { get; set; }

        public string adjusted_by { get; set; } = string.Empty;
        public string? remarks { get; set; }
    }
}