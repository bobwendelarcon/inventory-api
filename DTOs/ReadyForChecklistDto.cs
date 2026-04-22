namespace inventory_api.DTOs
{
    public class ReadyForChecklistDto
    {
        public long order_id { get; set; }
        public string order_no { get; set; } = string.Empty;
        public long order_line_id { get; set; }

        public string? customer_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string? route_name { get; set; }
        public DateTime? delivery_date { get; set; }

        public string product_id { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;

        public decimal required_qty { get; set; }
        public decimal allocated_qty { get; set; }
        public decimal remaining_qty { get; set; }
        public decimal dispatched_qty { get; set; }

        public string allocation_status { get; set; } = string.Empty;

        public decimal available_for_checklist { get; set; }
    }
}