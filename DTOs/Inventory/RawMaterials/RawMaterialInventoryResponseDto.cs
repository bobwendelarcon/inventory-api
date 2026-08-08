namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialInventoryResponseDto
    {
        public RawMaterialInventorySummaryDto Summary { get; set; }
            = new RawMaterialInventorySummaryDto();

        public List<RawMaterialInventoryListDto> Items { get; set; }
            = new List<RawMaterialInventoryListDto>();
    }
}