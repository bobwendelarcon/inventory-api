namespace inventory_api.DTOs
{
    public class DeliveryChecklistDetailsDto
    {
        public long checklist_id { get; set; }
        public string checklist_no { get; set; } = string.Empty;
        public DateTime? delivery_date { get; set; }
        public string? route_name { get; set; }
        public string? truck_name { get; set; }
        public string? driver_name { get; set; }
        public string? helper_name { get; set; }
        public string? status { get; set; }
        public string? remarks { get; set; }

        public List<DeliveryChecklistDetailsLineDto> lines { get; set; } = new();
    }

    public class DeliveryChecklistDetailsLineDto
    {
        public long checklist_line_id { get; set; }
        public long order_id { get; set; }
        public string order_no { get; set; } = string.Empty;
        public long order_line_id { get; set; }

        public string? customer_id { get; set; }
        public string? customer_name { get; set; }
        public string product_id { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;

        public string? lot_no { get; set; }
        public DateTime? manufacturing_date { get; set; }
        public DateTime? expiration_date { get; set; }

        public decimal required_qty { get; set; }
        public decimal allocated_qty { get; set; }
        public decimal checklist_qty { get; set; }
        public decimal released_qty { get; set; }
        public decimal remaining_qty { get; set; }

        public string? status { get; set; }
        public string? remarks { get; set; }
    }
}