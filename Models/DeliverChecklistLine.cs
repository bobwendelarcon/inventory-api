namespace inventory_api.Models
{
    public class DeliveryChecklistLine
    {
        public long checklist_line_id { get; set; }
        public long checklist_id { get; set; }

        public long order_id { get; set; }
        public string? order_no { get; set; }
        public long order_line_id { get; set; }

        public string? customer_id { get; set; }
        public string? customer_name { get; set; }

        public string product_id { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;

        public string? uom { get; set; }
        public string? pack_uom { get; set; }
        public decimal? pack_qty { get; set; }

        public decimal required_qty { get; set; }
        public decimal allocated_qty { get; set; }
        public decimal checklist_qty { get; set; }
        public decimal released_qty { get; set; }
        public decimal remaining_qty { get; set; }

        public string status { get; set; } = "READY";
        public string? remarks { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool is_deleted { get; set; }

        public string? lot_no { get; set; }
        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }

        public DeliveryChecklistHeader Header { get; set; } = null!;
    }
}