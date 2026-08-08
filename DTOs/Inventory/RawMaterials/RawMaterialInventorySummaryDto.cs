namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialInventorySummaryDto
    {
        public int TotalMaterials { get; set; }
        public int InventoryLots { get; set; }
        public int LowStock { get; set; }
        public int NearExpiry { get; set; }
        public int Expired { get; set; }
    }
}