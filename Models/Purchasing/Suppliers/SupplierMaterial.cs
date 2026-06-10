namespace inventory_api.Models.Purchasing.Suppliers
{
    public class SupplierMaterial
    {
        public int SupplierMaterialId { get; set; }

        public int SupplierId { get; set; }
        public int MaterialId { get; set; }
        public int? ManufacturerId { get; set; }

        public bool IsPreferred { get; set; }

        public string? Remarks { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
