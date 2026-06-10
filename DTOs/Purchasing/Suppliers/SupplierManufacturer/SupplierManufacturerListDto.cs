namespace inventory_api.DTOs.Purchasing.Suppliers.SupplierManufacturers
{
    public class SupplierManufacturerListDto
    {
        public int SupplierManufacturerId { get; set; }

        public int SupplierId { get; set; }

        public int ManufacturerId { get; set; }

        public string ManufacturerName { get; set; } = string.Empty;

        public string AccreditationStatus { get; set; } = string.Empty;

        public string CoaRequired { get; set; } = "N/A";
    }
}