namespace inventory_api.Models
{
    public class InventoryTransaction
    {
        public long transaction_id { get; set; }
        public string product_id { get; set; } = string.Empty;
        public string branch_id { get; set; } = string.Empty;
        public string transaction_type { get; set; } = string.Empty;
        public string lot_no { get; set; } = string.Empty;
        public decimal quantity { get; set; }
        public string scanned_by { get; set; } = string.Empty;
        public string? remarks { get; set; } = string.Empty;
        public string? supplier_id { get; set; } = "";
        public string? customer_id { get; set; } = "";
        public string? dr_no { get; set; } = "";
        public string? inv_no { get; set; } = "";
        public string? po_no { get; set; } = "";
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }

        public bool is_deleted { get; set; }
    }
}