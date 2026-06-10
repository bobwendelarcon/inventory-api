namespace inventory_api.DTOs.Purchasing.Suppliers.Mappings
{
    public class CreateSupplierMaterialDto
    {
        public int SupplierId { get; set; }

        public int MaterialId { get; set; }

        public int? ManufacturerId { get; set; }

        public bool IsPreferred { get; set; }

        public string? Remarks { get; set; }
    }
}