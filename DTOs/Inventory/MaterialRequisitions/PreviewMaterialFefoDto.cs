namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class PreviewMaterialFefoDto
    {
        public int MaterialId { get; set; }

        public string BranchId { get; set; } = string.Empty;

        public decimal RequestedQuantity { get; set; }
    }

    public class MaterialFefoAllocationDto
    {
        public int MaterialLotId { get; set; }

        public string? LotNo { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public decimal AllocatedQuantity { get; set; }
    }

    public class MaterialFefoPreviewResultDto
    {
        public int MaterialId { get; set; }

        public bool IsLotTracked { get; set; }

        public string AllocationMethod { get; set; } = string.Empty;

        public decimal RequestedQuantity { get; set; }

        public decimal AllocatedQuantity { get; set; }

        public decimal ShortageQuantity { get; set; }

        public List<MaterialFefoAllocationDto> Allocations
        { get; set; } = new();
    }
}