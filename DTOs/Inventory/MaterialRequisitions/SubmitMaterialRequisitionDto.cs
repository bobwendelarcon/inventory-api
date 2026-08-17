namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class SubmitMaterialRequisitionDto
    {
        public string SubmittedBy { get; set; } = string.Empty;
    }

    public class ApproveMaterialRequisitionDto
    {
        public string ApprovedBy { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    public class RejectMaterialRequisitionDto
    {
        public string RejectedBy { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
    }
}