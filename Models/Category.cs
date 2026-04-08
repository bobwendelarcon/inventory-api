namespace inventory_api.Models
{
    public class Branch
    {
        public string branch_id { get; set; } = string.Empty;
        public string branch_name { get; set; } = string.Empty;
        public string? branch_loc { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}