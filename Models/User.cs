namespace inventory_api.Models
{
    public class User
    {
        public string user_id { get; set; } = string.Empty;
        public string full_name { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string password_hash { get; set; } = string.Empty;
        public string role_name { get; set; } = string.Empty;
     //   public string? branch_id { get; set; }

        public bool is_deleted { get; set; }

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}