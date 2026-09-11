namespace inventory_api.DTOs.Purchasing.QaQcReceiving
{
    public class QaQcReceivingLotDto
    {
        public int IncomingReceivingLineLotId { get; set; }

        public int? ManufacturerId { get; set; }

        public string? ManufacturerName { get; set; }

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public decimal? ItemCount { get; set; }

        public decimal? Weight { get; set; }

        public string? Remarks { get; set; }
    }
}