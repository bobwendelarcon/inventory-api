namespace inventory_api.DTOs
{
    public class CreateReturnDto
    {
        public string? customer_id { get; set; }
        public string? customer_name { get; set; }
        public DateTime return_date { get; set; }
        public string? reason { get; set; }
        public string? remarks { get; set; }
        public string? created_by { get; set; }

        public List<CreateReturnLineDto> lines { get; set; } = new();
    }

    public class CreateReturnLineDto
    {
        public string product_id { get; set; } = "";
        public string product_name { get; set; } = "";
        public string branch_id { get; set; } = "";
        public string? lot_no { get; set; }
        public decimal quantity { get; set; }
        public string? uom { get; set; }
        public string? quarantine_location { get; set; }
        public string? condition_status { get; set; }
        public string? remarks { get; set; }

        public long? order_id { get; set; }
        public string? order_no { get; set; }
        public long? checklist_id { get; set; }
        public string? checklist_no { get; set; }
        public string? dr_no { get; set; }
        public string? inv_no { get; set; }

        public string? po_no { get; set; }

        public long? source_transaction_id { get; set; }
    }

    public class ReleaseReturnForReprocessDto
    {
        public List<ReleaseReturnLineDto> lines { get; set; } = new();
        public string? remarks { get; set; }
        public string? released_by { get; set; }
    }

    public class ReleaseReturnLineDto
    {
        public long return_line_id { get; set; }
        public decimal quantity { get; set; }
    }
}