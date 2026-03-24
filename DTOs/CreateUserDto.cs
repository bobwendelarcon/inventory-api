namespace inventory_api.DTOs
{
    public class CreateUserDto
    {
        public string user_id { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string password_hash { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty; // IN / OUT
        public string branch_id { get; set; } = string.Empty;
      

    }
}