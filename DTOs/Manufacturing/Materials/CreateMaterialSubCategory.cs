namespace inventory_api.DTOs.Manufacturing.Materials
{
    public class CreateMaterialSubCategoryDto
    {
        public int material_category_id { get; set; }

        public string subcategory_name { get; set; } = string.Empty;

        public string? description { get; set; }
    }
}