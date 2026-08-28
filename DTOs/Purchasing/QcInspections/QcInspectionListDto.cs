namespace inventory_api.DTOs.Purchasing.QcInspections
{
    public class QcInspectionListDto
    {
        public int QcId { get; set; }
        public string QcNo { get; set; } = string.Empty;

        public int? RrId { get; set; }
        public string? RrNo { get; set; }

        public int? IncomingReceivingId { get; set; }
        public string? IncomingNo { get; set; }

        public string PoNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;

        public DateTime? InspectionDate { get; set; }

        public string? InspectorId { get; set; }

        public string? InspectorName { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Decision { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? QuarantineId { get; set; }

        public string? QuarantineNo { get; set; }

        public string? QuarantineStatus { get; set; }
    }
}