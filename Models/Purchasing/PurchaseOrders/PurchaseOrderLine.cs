namespace inventory_api.Models.Purchasing.PurchaseOrders
{
    public class PurchaseOrderLine
    {
        public int PoLineId { get; set; }
        public int PoId { get; set; }

        public int CanvassLineId { get; set; }
        public int? QuoteId { get; set; }

        public int MaterialId { get; set; }

        public decimal PoQty { get; set; }
        public string Uom { get; set; } = string.Empty;

        public decimal? QuotationUnitPrice { get; set; }
        public decimal PoUnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public string? Remarks { get; set; }

        public decimal ReceivedQty { get; set; }
        public decimal BalanceQty { get; set; }

        public string Status { get; set; } = "OPEN";

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public PurchaseOrderHeader? Header { get; set; }
    }
}
