namespace inventory_api.Models
{
    public class UpdateMyAccountDto
    {
        public string full_name { get; set; } = "";
        public string username { get; set; } = "";
        public string? password_hash { get; set; }
    }
}