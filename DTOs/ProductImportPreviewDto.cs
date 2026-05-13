namespace inventory_api.DTOs
{
    public class ProductImportPreviewDto
    {
        public string SheetName { get; set; } = "";
        public string? CategoryId { get; set; }
        public bool CategoryExists { get; set; }
        public int ProductCount { get; set; }
    }

    public class ProductImportRequestDto
    {
        public string FileToken { get; set; } = "";
        public List<string> SelectedSheets { get; set; } = new();
    }
}
