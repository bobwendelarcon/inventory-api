namespace inventory_api.DTOs
{
    public class ChangePasswordDto
    {
        public string current_password { get; set; } = string.Empty;
        public string new_password { get; set; } = string.Empty;
        public string confirm_password { get; set; } = string.Empty;
    }
}