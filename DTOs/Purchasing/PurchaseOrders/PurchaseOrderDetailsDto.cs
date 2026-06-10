namespace inventory_api.DTOs.Purchasing.PurchaseOrders
{
    public class PurchaseOrderDetailsDto
    {
        public int PoId { get; set; }

        public string PoNo { get; set; } = string.Empty;

        public int CanvassId { get; set; }

        public int SupplierId { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public DateTime PoDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public string? PaymentTerms { get; set; }

        public string? Remarks { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? SupplierAddress { get; set; }
        public string? RequestedBy { get; set; }

        public decimal Subtotal { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal TotalAmount { get; set; }

        public string? CheckedBy { get; set; }
        public DateTime? CheckedAt { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? PrintedPoNo { get; set; }

        public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    }
}