namespace inventory_api.DTOs.Purchasing.ReceivingReports
{
    public class ReceivingReportDetailsDto
    {
        public int RrId { get; set; }
        public string RrNo { get; set; } = string.Empty;

        public int PoId { get; set; }
        public string PoNo { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public string? SiDrNo { get; set; }
        public DateTime DeliveryDate { get; set; }

        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? QcBy { get; set; }
        public DateTime? QcAt { get; set; }

        public string? CommittedBy { get; set; }
        public DateTime? CommittedAt { get; set; }

        public List<ReceivingReportLineDetailsDto> Lines { get; set; } = new();
    }

    public class ReceivingReportLineDetailsDto
    {
        public int RrLineId { get; set; }
        public int PoLineId { get; set; }
        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public decimal PoQty { get; set; }
        public decimal PreviouslyReceivedQty { get; set; }
        public decimal BalanceQty { get; set; }
        public decimal ReceiveQty { get; set; }

        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }

        public bool IsOverReceived { get; set; }
        public decimal OverReceivedQty { get; set; }

        public string Uom { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}