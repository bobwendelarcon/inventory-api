namespace inventory_api.DTOs.Purchasing.Quarantine
{
    public class RmwMaterialProcessingDto
    {
        public int QuarantineId { get; set; }
        public string QuarantineNo { get; set; } = string.Empty;

        public int QcId { get; set; }
        public string QcNo { get; set; } = string.Empty;

        public int? IncomingReceivingId { get; set; }
        public string? IncomingNo { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public List<RmwMaterialProcessingLineDto> Lines { get; set; } = new();
    }

    public class RmwMaterialProcessingLineDto
    {
        public int QuarantineLineId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public decimal AcceptedQty { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}