namespace inventory_api.DTOs
{
    public class CreateInventoryTransactionDto
    {
      //  public string transaction_id { get; set; } = string.Empty;

        public string product_id { get; set; } = string.Empty;
        public string branch_id { get; set; } = string.Empty;
        public string transaction_type { get; set; } = string.Empty; // IN / OUT
        public string lot_no { get; set; } = string.Empty;
        public double quantity { get; set; }
        public string scanned_by { get; set; } = string.Empty;
        public string remarks { get; set; } = string.Empty;
        public string partner { get; set; } = string.Empty;
    }
}