namespace inventory_api.DTOs.Purchasing.QcInspections
{
    public class PendingRawMaterialEvaluationDto
    {
        public int QuarantineId { get; set; }
        public string QuarantineNo { get; set; } = "";

        // QC evaluation information
        public int? QcId { get; set; }
        public string? QcNo { get; set; }
        public string? QcStatus { get; set; }
        public bool AlreadyStarted { get; set; }

        public int IncomingReceivingId { get; set; }
        public string IncomingNo { get; set; } = "";

        public int PoId { get; set; }
        public string PoNo { get; set; } = "";

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = "";

        public string Status { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public List<PendingRawMaterialEvaluationLineDto> Lines
        { get; set; } = new();
    }
}