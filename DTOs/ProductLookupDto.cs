namespace inventory_api.DTOs
{
    public class ProductLookupDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryId { get; set; }

        public string? Uom { get; set; }
        public string? PackUom { get; set; }
        public decimal? PackQty { get; set; }
    }
}
