namespace inventory_api.DTOs.Manufacturing.Materials
{
    public class CreateMaterialCategoryDto
    {
        public string category_name { get; set; } = string.Empty;
        public string? description { get; set; }
    }
}