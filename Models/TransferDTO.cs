namespace inventory_api.DTOs
{
    public class TransferDto
    {
        public string product_id { get; set; } = string.Empty;
        public string lot_no { get; set; } = string.Empty;

        public string from_branch { get; set; } = string.Empty;
        public string to_branch { get; set; } = string.Empty;

        public decimal quantity { get; set; }

        public string? scanned_by { get; set; }
        public string? remarks { get; set; }
    }
}