namespace inventory_api.Models.Purchasing.PurchaseOrders
{
    public class PurchaseOrderHeader
    {
        public int PoId { get; set; }
        public string PoNo { get; set; } = string.Empty;

        public int CanvassId { get; set; }
        public int SupplierId { get; set; }

        public DateTime PoDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        public string? PaymentTerms { get; set; }
        public string? Remarks { get; set; }

        public string Status { get; set; } = "DRAFT";

        public string CreatedBy { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }

        public string? SupplierAddress { get; set; }
        public string? RequestedBy { get; set; }

        public decimal Subtotal { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal TotalAmount { get; set; }

        public string? CheckedBy { get; set; }
        public DateTime? CheckedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? PrintedPoNo { get; set; }

        public List<PurchaseOrderLine> Lines { get; set; } = new();
    }
}