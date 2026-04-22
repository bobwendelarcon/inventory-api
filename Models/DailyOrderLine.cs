namespace inventory_api.Models
{
    public class DailyOrderLine
    {
        public long order_line_id { get; set; }
        public long order_id { get; set; }

        public string? product_id { get; set; }
        public string product_name { get; set; } = string.Empty;

        public decimal required_qty { get; set; }
        public decimal allocated_qty { get; set; }
        public decimal remaining_qty { get; set; }

        public string allocation_status { get; set; } = "Not Allocated";
        public DateTime created_at { get; set; }
        public decimal dispatched_qty { get; set; }
        public string status { get; set; } = "PENDING";
        public DateTime? updated_at { get; set; }

        public DailyOrderHeader Header { get; set; } = null!;
        public ICollection<DailyOrderAllocation> Allocations { get; set; } = new List<DailyOrderAllocation>();
    }
}