namespace inventory_api.Models.Purchasing.RawMaterialProcessing
{
    public class RmwProcessingHeader
    {
        public int ProcessingId { get; set; }

        public string ProcessingNo { get; set; } = string.Empty;

        public int QuarantineId { get; set; }

        public int QcId { get; set; }

        public int? IncomingReceivingId { get; set; }

        public int PoId { get; set; }

        public int SupplierId { get; set; }

        public string Status { get; set; } = "READY_FOR_WEIGHING";

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? CompletedBy { get; set; }

        public DateTime? CompletedAt { get; set; }

        public ICollection<RmwProcessingLine> Lines { get; set; }
            = new List<RmwProcessingLine>();
    }
}