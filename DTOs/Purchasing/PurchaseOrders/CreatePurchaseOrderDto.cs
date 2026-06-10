namespace inventory_api.DTOs.Purchasing.PurchaseOrders
{
    public class CreatePurchaseOrderDto
    {
        public int CanvassId { get; set; }

        public int SupplierId { get; set; }

        public DateTime PoDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public string? PaymentTerms { get; set; }

        public string? SupplierAddress { get; set; }

        public string? RequestedBy { get; set; }

        public decimal OtherCharges { get; set; }

        public string? Remarks { get; set; }
        public string? PrintedPoNo { get; set; }
        public string? CreatedBy { get; set; }

        public List<CreatePurchaseOrderLineDto> Lines { get; set; } = new();
    }

    public class CreatePurchaseOrderLineDto
    {
        public int CanvassLineId { get; set; }

        public int? QuoteId { get; set; }

        public int MaterialId { get; set; }

        public decimal PoQty { get; set; }

        public string Uom { get; set; } = string.Empty;

        public decimal? QuotationUnitPrice { get; set; }

        public decimal PoUnitPrice { get; set; }

        public string? Remarks { get; set; }
    }
}