using System.Text.Json.Serialization;
namespace inventory_api.Models
{
    public class ReturnLine
    {
        public long return_line_id { get; set; }
        public long return_id { get; set; }

        public string product_id { get; set; } = "";
        public string product_name { get; set; } = "";
        public string branch_id { get; set; } = "";
        public string? lot_no { get; set; }
        public decimal quantity { get; set; }
        public string? uom { get; set; }

        public string? quarantine_location { get; set; }
        public string condition_status { get; set; } = "FOR INSPECTION";
        public string release_status { get; set; } = "IN QUARANTINE";
        public decimal released_qty { get; set; } = 0;
        public string? remarks { get; set; }

        public long? order_id { get; set; }
        public string? order_no { get; set; }
        public long? checklist_id { get; set; }
        public string? checklist_no { get; set; }
        public string? dr_no { get; set; }
        public string? inv_no { get; set; }

        public string? po_no { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }

        public long? source_transaction_id { get; set; }

        [JsonIgnore]
        public ReturnHeader? ReturnHeader { get; set; }
    }
}