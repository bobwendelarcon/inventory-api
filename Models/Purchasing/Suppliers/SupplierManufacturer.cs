namespace inventory_api.Models.Purchasing.Suppliers
{
    public class SupplierManufacturer
    {
        public int SupplierManufacturerId { get; set; }

        public int SupplierId { get; set; }
        public int ManufacturerId { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}