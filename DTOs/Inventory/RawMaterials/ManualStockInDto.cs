namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class ManualStockInDto
    {
        public int MaterialId { get; set; }

        public string BranchId { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public int? SupplierId { get; set; }

        public string? Remarks { get; set; }

        public string EncodedBy { get; set; } = string.Empty;
    }
}