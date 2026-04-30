namespace inventory_api.DTOs
{
    public class ManualStockInDto
    {
        public string branch_id { get; set; } = "";
        public string product_id { get; set; } = "";
        public string? supplier_id { get; set; }

        public decimal quantity { get; set; }

        public string lot_no { get; set; } = "";
        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }

        public string? reference_type { get; set; }
        public string? dr_no { get; set; }
        public string? inv_no { get; set; }
        public string? po_no { get; set; }

        public string? remarks { get; set; }
        public string? scanned_by { get; set; }
    }
}