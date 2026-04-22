namespace inventory_api.DTOs
{
    public class ChecklistDetailLineDto
    {
        public long checklist_line_id { get; set; }
        public long order_id { get; set; }
        public string? order_no { get; set; }

        public long order_line_id { get; set; }

        public string? customer_id { get; set; }
        public string? customer_name { get; set; }

        public string product_id { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;

        public string? branch_id { get; set; }   // ✅ NEW

        public string? uom { get; set; }
        public string? pack_uom { get; set; }
        public decimal? pack_qty { get; set; }

        public string lot_no { get; set; } = string.Empty;

        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }

        public decimal required_qty { get; set; }
        public decimal allocated_qty { get; set; }
        public decimal checklist_qty { get; set; }
        public decimal released_qty { get; set; }
        public decimal remaining_qty { get; set; }

        public string status { get; set; } = string.Empty;
        public string? remarks { get; set; }
    }
}