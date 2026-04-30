namespace inventory_api.DTOs
{
    public class ReturnOutTransactionSearchDto
    {
        public long transaction_id { get; set; }
        public DateTime created_at { get; set; }

        public string product_id { get; set; } = "";
        public string product_name { get; set; } = "";
        public string branch_id { get; set; } = "";
        public string branch_name { get; set; } = "";

        public string lot_no { get; set; } = "";
        public decimal quantity { get; set; }
        public string? uom { get; set; }

        public string? customer_id { get; set; }
        public string? customer_name { get; set; }

        public string? dr_no { get; set; }
        public string? inv_no { get; set; }
        public string? po_no { get; set; }
        public string? remarks { get; set; }

        public long? order_id { get; set; }
        public string? order_no { get; set; }

        public long? checklist_id { get; set; }
        public string? checklist_no { get; set; }

        public decimal returned_qty { get; set; }
        public decimal returnable_qty { get; set; }
    }
}