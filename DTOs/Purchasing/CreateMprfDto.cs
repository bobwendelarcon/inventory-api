namespace inventory_api.DTOs.Purchasing
{
    public class CreateMprfDto
    {
 
        public string? mprf_no { get; set; }
        public string? category { get; set; }
        public DateTime request_date { get; set; }
        public string? week { get; set; }
        public string? requested_by { get; set; }

        public List<CreateMprfLineDto> lines { get; set; } = new();
    }
}