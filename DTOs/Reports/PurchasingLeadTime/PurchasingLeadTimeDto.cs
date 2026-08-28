namespace inventory_api.DTOs.Reports.PurchasingLeadTime
{
    public class PurchasingLeadTimeDto
    {
        public int MprfId { get; set; }
        public string MprfNo { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? RequestedBy { get; set; }
        public string? RequestedByName { get; set; }

        public string? SubmittedBy { get; set; }
        public string? SubmittedByName { get; set; }

        public DateTime? MprfSubmittedAt { get; set; }

        public int PoId { get; set; }
        public string PoNo { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public string PoStatus { get; set; } = string.Empty;

        public string? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }

        public DateTime? PoApprovedAt { get; set; }

        public int? IncomingReceivingId { get; set; }
        public string? IncomingNo { get; set; }

        public DateTime? IncomingReceivedAt { get; set; }

        public string? IncomingReceivedBy { get; set; }

        public string? IncomingReceivedByName { get; set; }

        public double? PurchasingLeadTimeMinutes { get; set; }

        public double? DeliveryLeadTimeMinutes { get; set; }

        public double? OverallLeadTimeMinutes { get; set; }


        public DateTime? MprfReviewedAt { get; set; }

        public string? ReviewedBy { get; set; }

        public string? ReviewedByName { get; set; }

        public double? MprfReviewMinutes { get; set; }

        public double? PoProcessingMinutes { get; set; }

        public string CurrentStage { get; set; } = string.Empty;
    }
}