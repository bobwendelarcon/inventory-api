namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialLotDto
    {
        public int MaterialLotId { get; set; }

        public int MaterialId { get; set; }

        public string BranchId { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public string LotNo { get; set; } = string.Empty;

        public string LotDisplay { get; set; } = string.Empty;

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = string.Empty;

        public int? SupplierId { get; set; }

        public string SupplierName { get; set; } = string.Empty;
    }
}