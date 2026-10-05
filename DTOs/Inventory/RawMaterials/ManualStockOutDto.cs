namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class ManualStockOutDto
    {
        public int MaterialLotId { get; set; }

        public decimal Quantity { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string EncodedBy { get; set; } = string.Empty;
    }
}