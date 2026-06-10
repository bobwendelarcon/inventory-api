namespace inventory_api.DTOs.Purchasing.Canvassing
{
    public class UpdateCanvassQuoteDto
    {
        public int SupplierId { get; set; }
        public int? ManufacturerId { get; set; }
        public decimal UnitPrice { get; set; }
        public string? PaymentTerms { get; set; }
        public int? DeliveryDays { get; set; }
        public bool CoaAvailable { get; set; }
        public string? DocumentsRemarks { get; set; }
        public string? QuotationRef { get; set; }
        public DateTime? QuoteDate { get; set; }
        public string? Remarks { get; set; }
    }
}