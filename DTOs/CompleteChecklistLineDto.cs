namespace inventory_api.DTOs
{
    public class CompleteChecklistLineDto
    {
        public long checklist_id { get; set; }
        public long checklist_line_id { get; set; }

        public string product_id { get; set; } = "";
        public string? lot_no { get; set; }
        public string? branch_id { get; set; }

        public string adjustment_type { get; set; } = "DEDUCT";
        public decimal quantity { get; set; }

        public string? adjusted_by { get; set; }
        public string? reference_type { get; set; }

        public string? dr_no { get; set; }
        public string? inv_no { get; set; }
        public string? po_no { get; set; }
        public string? remarks { get; set; }
    }
}