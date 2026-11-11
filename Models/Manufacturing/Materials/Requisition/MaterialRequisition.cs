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


        public string? SubmittedBy { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovalRemarks { get; set; }

        // Warehouse Staff - First Approval
        public string? StaffApprovedBy { get; set; }
        public DateTime? StaffApprovedAt { get; set; }
        public string? StaffApprovalRemarks { get; set; }


        
public string? WarehouseConfirmedBy { get; set; }

public DateTime? WarehouseConfirmedAt { get; set; }


        // Warehouse Head - Final Approval
        public string? HeadApprovedBy { get; set; }
        public DateTime? HeadApprovedAt { get; set; }
        public string? HeadApprovalRemarks { get; set; }





        public string? RejectedBy { get; set; }

        public DateTime? RejectedAt { get; set; }

        public string? RejectionReason { get; set; }



        public string? PostedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PostedAt { get; set; }

        public ICollection<MaterialRequisitionLine> Lines { get; set; }
            = new List<MaterialRequisitionLine>();
    }
}