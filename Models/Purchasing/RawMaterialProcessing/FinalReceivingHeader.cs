namespace inventory_api.Models.Purchasing.FinalReceiving
{
    public class FinalReceivingHeader
    {
        public int FinalRrId { get; set; }

        public string FinalRrNo { get; set; } = string.Empty;

        public int ProcessingId { get; set; }

        public int QuarantineId { get; set; }

        public int QcId { get; set; }

        public int? IncomingReceivingId { get; set; }

        public int PoId { get; set; }

        public int SupplierId { get; set; }

        public string Status { get; set; } = "DRAFT";

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? CommittedBy { get; set; }

        public DateTime? CommittedAt { get; set; }

        public ICollection<FinalReceivingLine> Lines { get; set; }
            = new List<FinalReceivingLine>();
    }
}