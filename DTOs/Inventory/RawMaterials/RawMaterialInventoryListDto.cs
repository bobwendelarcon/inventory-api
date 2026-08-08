namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialInventoryListDto
    {
        public int MaterialLotId { get; set; }

        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public bool IsLotTracked { get; set; }

        // Internal database lot reference.
        public string LotNo { get; set; } = string.Empty;

        // Friendly value for the grid.
        public string LotDisplay { get; set; } = string.Empty;

        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public decimal Quantity { get; set; }
        public string Uom { get; set; } = string.Empty;

        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public decimal MinimumStock { get; set; }

        public string StockStatus { get; set; } = string.Empty;
        public string ExpiryStatus { get; set; } = string.Empty;

        public int InventoryAgeDays { get; set; }
        public int? DaysUntilExpiration { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}