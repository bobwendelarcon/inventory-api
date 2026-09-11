namespace inventory_api.DTOs.Purchasing.QcInspections
{
    public class PendingRawMaterialEvaluationLineDto
    {
        public int QuarantineLineId { get; set; }

        public int QaReceivingLineId { get; set; }

        public int? IncomingReceivingLineId { get; set; }

        public int? IncomingReceivingLineLotId { get; set; }

        public int PoLineId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";

        public bool IsLotTracked { get; set; }

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public decimal ReceivedQty { get; set; }

        public string Status { get; set; } = "";
    }
}