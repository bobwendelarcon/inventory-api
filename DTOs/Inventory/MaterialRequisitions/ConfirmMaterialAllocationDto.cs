
namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class ConfirmMaterialAllocationDto
    {
        public string ConfirmedBy { get; set; } = "";

        public List<ConfirmMaterialAllocationLineDto> Lines
        { get; set; } = new();
    }

    public class ConfirmMaterialAllocationLineDto
    {
        public int RequisitionLineId { get; set; }

        public List<ConfirmMaterialAllocationLotDto> Allocations
        { get; set; } = new();
    }

    public class ConfirmMaterialAllocationLotDto
    {
        public int? MaterialLotId { get; set; }

        public decimal Quantity { get; set; }

        public string? Remarks { get; set; }
    }
}
