namespace inventory_api.DTOs.Purchasing.Suppliers.Mappings
{
    public class SupplierMaterialListDto
    {
        public int SupplierMaterialId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = "";

        public string MaterialName { get; set; } = "";

        public int? ManufacturerId { get; set; }

        public string? ManufacturerName { get; set; }

        public bool IsPreferred { get; set; }

        public string? Remarks { get; set; }
    }
}