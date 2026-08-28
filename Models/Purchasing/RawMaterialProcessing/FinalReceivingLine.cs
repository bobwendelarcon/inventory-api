namespace inventory_api.Models.Purchasing.FinalReceiving
{
    public class FinalReceivingLine
    {
        public int FinalRrLineId { get; set; }

        public int FinalRrId { get; set; }

        public int ProcessingLineId { get; set; }

        public int QuarantineLineId { get; set; }

        public int MaterialId { get; set; }

        public string? LotNo { get; set; }

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public decimal QaAcceptedQty { get; set; }

        public decimal ActualQty { get; set; }

        public decimal VarianceQty { get; set; }

        public string? Uom { get; set; }

        public FinalReceivingHeader? Header { get; set; }
    }
}