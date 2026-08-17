namespace inventory_api.DTOs.Inventory.MaterialRequisitions
{
    public class MaterialRequisitionDetailsDto
    {
        public int RequisitionId { get; set; }

        public string RequisitionNo { get; set; } = string.Empty;

        public string BranchId { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public DateTime RequisitionDate { get; set; }

        public string? RequestedBy { get; set; }

        public string? ReleasedBy { get; set; }

        public string? ReceivedBy { get; set; }

        public string? VerifiedBy { get; set; }

        public DateTime? TimeRequested { get; set; }

        public DateTime? TimeServed { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public string? SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalRemarks { get; set; }

        public string? RejectedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }

        public string? PostedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PostedAt { get; set; }

        public List<MaterialRequisitionDetailsLineDto> Lines { get; set; }
            = new();
    }

    public class MaterialRequisitionDetailsLineDto
    {
        public int RequisitionLineId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public int? MaterialLotId { get; set; }

        public string? LotNo { get; set; }

        public string LotDisplay { get; set; } = string.Empty;

        public DateTime? ExpirationDate { get; set; }

        public decimal RequestedQuantity { get; set; }

        public decimal? ActualQuantity { get; set; }

        public decimal AvailableQuantity { get; set; }

        public string Uom { get; set; } = string.Empty;

        public string? Remarks { get; set; }



        public bool IsLotTracked { get; set; }
    }
}