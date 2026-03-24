namespace inventory_api.DTOs
{
    public class CreatePartnerDto
    {
        public string partner_id { get; set; } = string.Empty;
        public string partner_name { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public string contact_no { get; set; } = string.Empty; // IN / OUT
        public string partner_type { get; set; } = string.Empty;
      


    }
}