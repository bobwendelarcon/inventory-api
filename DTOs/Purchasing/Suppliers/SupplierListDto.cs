namespace inventory_api.DTOs.Purchasing.Suppliers
{
    public class SupplierListDto
    {
        public int SupplierId { get; set; }

        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty;

        public string? ContactPerson { get; set; }

        public string? PaymentTerms { get; set; }

        public int LeadTimeDays { get; set; }

        public bool IsPreferred { get; set; }

        public bool IsActive { get; set; }
    }
}
