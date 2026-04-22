namespace inventory_api.DTOs
{
    public class ChecklistOutRequestDto
    {
        public long checklist_id { get; set; }
        public long checklist_line_id { get; set; } // ✅ ADD THIS

        public string lot_no { get; set; } = string.Empty;
        public decimal quantity { get; set; }

        public string? dr_no { get; set; }
        public string? inv_no { get; set; }
        public string? po_no { get; set; }

        public string? customer_id { get; set; }
        public string? customer_name { get; set; }
        public string? order_no { get; set; }

        public string scanned_by { get; set; } = string.Empty;
        public string? remarks { get; set; }
    }
}