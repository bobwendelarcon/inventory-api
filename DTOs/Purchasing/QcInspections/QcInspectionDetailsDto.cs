namespace inventory_api.DTOs.Purchasing.QcInspections
{
    public class QcInspectionDetailsDto
    {
        public int QcId { get; set; }
        public string QcNo { get; set; } = string.Empty;

        public int RrId { get; set; }
        public string RrNo { get; set; } = string.Empty;

        public int PoId { get; set; }
        public string PoNo { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public DateTime? InspectionDate { get; set; }
        public string? InspectorId { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? Decision { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? CommittedBy { get; set; }
        public DateTime? CommittedAt { get; set; }

        public List<QcInspectionLineDetailsDto> Lines { get; set; } = new();
    }

    public class QcInspectionLineDetailsDto
    {
        public int QcLineId { get; set; }
        public int RrLineId { get; set; }
        public int PoLineId { get; set; }
        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public decimal ReceivedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }

        public string? RejectionReason { get; set; }
        public string? Remarks { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}