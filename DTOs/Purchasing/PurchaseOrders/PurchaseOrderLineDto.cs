namespace inventory_api.DTOs.Purchasing.PurchaseOrders
{
    public class PurchaseOrderLineDto
    {
        public int PoLineId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public decimal PoQty { get; set; }

        public string Uom { get; set; } = string.Empty;

        public decimal? QuotationUnitPrice { get; set; }

        public decimal PoUnitPrice { get; set; }

        public decimal LineTotal { get; set; }

        public decimal ReceivedQty { get; set; }

        public decimal BalanceQty { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }
}