namespace inventory_api.DTOs
{
    public class CreateChecklistDto
    {
        public DateTime delivery_date { get; set; }
        public string? route_name { get; set; }
        public string? truck_name { get; set; }
        public string? driver_name { get; set; }
        public string? helper_name { get; set; }
        public string? remarks { get; set; }
        public string? created_by { get; set; }

        public List<ChecklistLineDto> lines { get; set; } = new();
    }

    public class ChecklistLineDto
    {
        public long order_id { get; set; }
        public string order_no { get; set; } = string.Empty;
        public long order_line_id { get; set; }
        public string? customer_id { get; set; }
        public string? customer_name { get; set; }

        public string product_id { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;

        public decimal required_qty { get; set; }
        public decimal allocated_qty { get; set; }
        public decimal checklist_qty { get; set; }
    }
}