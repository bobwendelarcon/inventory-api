namespace inventory_api.DTOs
{
    public class ChecklistOutResponseDto
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;

        public long checklist_id { get; set; }
        public long checklist_line_id { get; set; }
        public long order_id { get; set; }
        public long order_line_id { get; set; }

        public string product_id { get; set; } = string.Empty;
        public string lot_no { get; set; } = string.Empty;
        public string? branch_id { get; set; }   // ✅ NEW
        public decimal released_qty { get; set; }
        public decimal checklist_remaining_qty { get; set; }
        public decimal dispatched_qty { get; set; }

        public string checklist_status { get; set; } = string.Empty;
        public string order_line_status { get; set; } = string.Empty;
        public string order_status { get; set; } = string.Empty;
    }
}