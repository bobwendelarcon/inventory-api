namespace inventory_api.Models.Purchasing.Suppliers
{
    public class Manufacturer
    {
        public int ManufacturerId { get; set; }

        public string ManufacturerName { get; set; } = string.Empty;

        public string AccreditationStatus { get; set; } = "For Evaluation";

        public DateTime? AccreditationDate { get; set; }
        public DateTime? AccreditationExpiry { get; set; }

        public string CoaRequired { get; set; } = "N/A";

        public string? Remarks { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
