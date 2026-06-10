namespace inventory_api.DTOs.Purchasing.Suppliers.Manufacturers
{
    public class CreateManufacturerDto
    {
        public string ManufacturerName { get; set; } = string.Empty;

        public string AccreditationStatus { get; set; } = "For Evaluation";

        public DateTime? AccreditationDate { get; set; }
        public DateTime? AccreditationExpiry { get; set; }

        public string CoaRequired { get; set; } = "N/A";

        public string? Remarks { get; set; }
    }
}