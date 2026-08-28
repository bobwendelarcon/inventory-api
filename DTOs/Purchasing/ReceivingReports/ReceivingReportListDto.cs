namespace inventory_api.DTOs.Purchasing.ReceivingReports
{
    public class ReceivingReportListDto
    {
        public int RrId { get; set; }
        public string RrNo { get; set; } = string.Empty;

        public int? ScheduleId { get; set; }

        public int? IncomingReceivingId { get; set; }
        public string? IncomingNo { get; set; }

        public int? QcId { get; set; }
        public string? QcNo { get; set; }

        public int? ProcessingId { get; set; }
        public string? ProcessingNo { get; set; }

        public int PoId { get; set; }

        public string PoNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;

        public string? SiDrNo { get; set; }
        public DateTime DeliveryDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? CommittedBy { get; set; }
        public DateTime? CommittedAt { get; set; }
    }
}