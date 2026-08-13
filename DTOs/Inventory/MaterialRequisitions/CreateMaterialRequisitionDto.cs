namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class CreateMaterialRequisitionDto
    {
        public string BranchId { get; set; } = string.Empty;

        public DateTime RequisitionDate { get; set; }

        public string? RequestedBy { get; set; }

        public DateTime? TimeRequested { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public List<CreateMaterialRequisitionLineDto> Lines { get; set; }
            = new();
    }

    public class CreateMaterialRequisitionLineDto
    {
        public int MaterialId { get; set; }

        public int? MaterialLotId { get; set; }

        public string? LotNo { get; set; }

        public decimal RequestedQuantity { get; set; }

        public string? Remarks { get; set; }
    }
}