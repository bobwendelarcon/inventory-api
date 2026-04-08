namespace inventory_api.Models
{
    public class Category
    {
        public string catg_id { get; set; } = string.Empty;
        public string catg_name { get; set; } = string.Empty;
        public string? catg_desc { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}