namespace inventory_api.Models
{
    public class ReturnHeader
    {
        public long return_id { get; set; }
        public string return_no { get; set; } = "";
        public string? customer_id { get; set; }
        public string? customer_name { get; set; }
        public DateTime return_date { get; set; }
        public string status { get; set; } = "QUARANTINE";
        public string? reason { get; set; }
        public string? remarks { get; set; }
        public string? created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public bool is_deleted { get; set; } = false;

        public List<ReturnLine> Lines { get; set; } = new();
    }
}