namespace inventory_api.Models.Manufacturing.Materials.Requisitions
{
    public class MaterialRequisition
    {
        public int RequisitionId { get; set; }

        public string RequisitionNo { get; set; } = string.Empty;

        public string BranchId { get; set; } = string.Empty;

        public DateTime RequisitionDate { get; set; }

        public string? RequestedBy { get; set; }

        public string? ReleasedBy { get; set; }

        public string? ReceivedBy { get; set; }

        public string? VerifiedBy { get; set; }

        public DateTime? TimeRequested { get; set; }

        public DateTime? TimeServed { get; set; }

        public string Status { get; set; } = "DRAFT";

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public string? PostedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PostedAt { get; set; }

        public ICollection<MaterialRequisitionLine> Lines { get; set; }
            = new List<MaterialRequisitionLine>();
    }
}