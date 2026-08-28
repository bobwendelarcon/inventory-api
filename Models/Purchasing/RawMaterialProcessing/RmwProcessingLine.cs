namespace inventory_api.Models.Purchasing.RawMaterialProcessing
{
    public class RmwProcessingLine
    {
        public int ProcessingLineId { get; set; }

        public int ProcessingId { get; set; }

        public int QuarantineLineId { get; set; }

        public int QcLineId { get; set; }

        public int? QcLineLotId { get; set; }

        public int? IncomingReceivingLineId { get; set; }

        public int PoLineId { get; set; }

        public int MaterialId { get; set; }

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public decimal QaAcceptedQty { get; set; }

        public decimal? ActualQty { get; set; }

        public decimal? VarianceQty { get; set; }

        public string? Uom { get; set; }

        public string Status { get; set; } = "READY_FOR_WEIGHING";

        public string? WeighingStartedBy { get; set; }

        public DateTime? WeighingStartedAt { get; set; }

        public string? WeighingCompletedBy { get; set; }

        public DateTime? WeighingCompletedAt { get; set; }

        public string? StickerCompletedBy { get; set; }

        public DateTime? StickerCompletedAt { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public RmwProcessingHeader? Header { get; set; }
    }
}