namespace inventory_api.DTOs.Purchasing.RawMaterialProcessing
{
    public class RmwProcessingListDto
    {
        public int ProcessingId { get; set; }
        public string ProcessingNo { get; set; } = string.Empty;

        public int QuarantineId { get; set; }
        public string QuarantineNo { get; set; } = string.Empty;

        public int? QcId { get; set; }
        public string QcNo { get; set; } = string.Empty;

        public int? IncomingReceivingId { get; set; }
        public string? IncomingNo { get; set; }

        public int PoId { get; set; }
        public string PoNo { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public List<RmwProcessingLineDto> Lines { get; set; } = new();
    }

    public class RmwProcessingLineDto
    {
        public int ProcessingLineId { get; set; }
        public int QuarantineLineId { get; set; }

        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public decimal QaAcceptedQty { get; set; }
        public decimal? ActualQty { get; set; }
        public decimal? VarianceQty { get; set; }

        public string? Uom { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? WeighingStartedBy { get; set; }
        public DateTime? WeighingStartedAt { get; set; }

        public string? WeighingCompletedBy { get; set; }
        public DateTime? WeighingCompletedAt { get; set; }

        public string? StickerCompletedBy { get; set; }
        public DateTime? StickerCompletedAt { get; set; }
    }
}