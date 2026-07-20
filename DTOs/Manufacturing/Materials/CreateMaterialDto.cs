namespace inventory_api.DTOs.Manufacturing.Materials
{
    public class CreateMaterialDto
    {
        public string material_code { get; set; } = string.Empty;
        public string material_name { get; set; } = string.Empty;

        public int? material_category_id { get; set; }
        public int? material_subcategory_id { get; set; }

        public string uom { get; set; } = string.Empty;

        public string? pack_uom { get; set; }
        public decimal? pack_qty { get; set; }
        public decimal minimum_stock { get; set; } = 0;

        public string? description { get; set; }

        public bool is_lot_tracked { get; set; } = false;
    }
}