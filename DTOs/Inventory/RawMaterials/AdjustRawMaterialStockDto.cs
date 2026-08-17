namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class AdjustRawMaterialStockDto
    {
        public int MaterialLotId { get; set; }

        // INCREASE or DECREASE
        public string AdjustmentType { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string EncodedBy { get; set; } = string.Empty;
    }
}