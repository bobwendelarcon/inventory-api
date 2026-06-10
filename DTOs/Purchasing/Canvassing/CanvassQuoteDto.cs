namespace inventory_api.DTOs.Purchasing.Canvassing
{
    public class CanvassQuoteDto
    {
        public int QuoteId { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierAddress { get; set; }
        public int? ManufacturerId { get; set; }
        public string? ManufacturerName { get; set; }
        public decimal UnitPrice { get; set; }
        public string? PaymentTerms { get; set; }
        public int? DeliveryDays { get; set; }
        public bool CoaAvailable { get; set; }
        public string? DocumentsRemarks { get; set; }
        public string? Remarks { get; set; }
        public bool IsRecommended { get; set; }
        public string? RecommendationReason { get; set; }
    }
}
