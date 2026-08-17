namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class PostMaterialRequisitionDto
    {
        public string ReleasedBy { get; set; } = string.Empty;

        public string ReceivedBy { get; set; } = string.Empty;

        public string VerifiedBy { get; set; } = string.Empty;

        public DateTime? TimeServed { get; set; }

        public string PostedBy { get; set; } = string.Empty;

        public List<PostMaterialRequisitionLineDto> Lines { get; set; }
            = new();
    }

    public class PostMaterialRequisitionLineDto
    {
        public int RequisitionLineId { get; set; }

        public decimal ActualQuantity { get; set; }
    }
}