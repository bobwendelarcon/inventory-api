namespace inventory_api.Models
{
    public class DailyOrderAllocation
    {
        public long allocation_id { get; set; }
        public long order_line_id { get; set; }

        public string? product_id { get; set; }
        public string lot_no { get; set; } = string.Empty;

        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }

        public decimal on_hand_qty { get; set; }
        public decimal reserved_qty { get; set; }
        public decimal available_qty { get; set; }
        public decimal allocated_qty { get; set; }

        public int priority_rank { get; set; }
        public DateTime created_at { get; set; }

        public DailyOrderLine Line { get; set; } = null!;
    }
}