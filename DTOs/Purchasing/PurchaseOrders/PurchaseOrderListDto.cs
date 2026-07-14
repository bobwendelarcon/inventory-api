namespace inventory_api.DTOs.Purchasing.PurchaseOrders
{
    public class PurchaseOrderListDto
    {
        public int PoId { get; set; }

        public string PoNo { get; set; } = string.Empty;

        public DateTime PoDate { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string CreatedByName { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}