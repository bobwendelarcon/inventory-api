namespace inventory_api.DTOs
{
    public class UserAccountDto
    {
        public string user_id { get; set; } = "";
        public string full_name { get; set; } = "";
        public string username { get; set; } = "";
        public string role_name { get; set; } = "";
        public string? profile_image { get; set; }
    }
}