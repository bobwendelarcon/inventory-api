namespace inventory_api.DTOs.Purchasing.Suppliers.Manufacturers
{
    public class ManufacturerDetailsDto
    {
        public int ManufacturerId { get; set; }
        public string ManufacturerName { get; set; } = string.Empty;
        public string AccreditationStatus { get; set; } = string.Empty;
        public DateTime? AccreditationDate { get; set; }
        public DateTime? AccreditationExpiry { get; set; }
        public string CoaRequired { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}