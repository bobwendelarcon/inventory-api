namespace inventory_api.DTOs.Purchasing.Suppliers
{
    public class CreateSupplierDto
    {
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty;

        public string? ContactPerson { get; set; }
        public string? ContactNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? Address { get; set; }

        public string? PaymentTerms { get; set; }
        public int LeadTimeDays { get; set; }

        public string Currency { get; set; } = "PHP";

        public bool IsPreferred { get; set; }

        public string? Remarks { get; set; }
    }
}
