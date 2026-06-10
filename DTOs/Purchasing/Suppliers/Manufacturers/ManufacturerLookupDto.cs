namespace inventory_api.DTOs.Purchasing.Suppliers.Manufacturers
{
    public class ManufacturerLookupDto
    {
        public int ManufacturerId { get; set; }
        public string ManufacturerName { get; set; } = string.Empty;
        public string AccreditationStatus { get; set; } = string.Empty;
    }
}