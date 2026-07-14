namespace inventory_api.Models.Purchasing.ReceivingReports
{
    public class ReceivingReportLine
    {
        public int RrLineId { get; set; }
        public int RrId { get; set; }

        public int PoLineId { get; set; }
        public int MaterialId { get; set; }

        public decimal PoQty { get; set; }
        public decimal PreviouslyReceivedQty { get; set; }
        public decimal BalanceQty { get; set; }
        public decimal ReceiveQty { get; set; }

        public bool IsOverReceived { get; set; }
        public decimal OverReceivedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }

        public string Uom { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ReceivingReportHeader? Header { get; set; }
    }
}