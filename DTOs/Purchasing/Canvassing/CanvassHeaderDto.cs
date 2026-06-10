namespace inventory_api.DTOs.Purchasing.Canvassing
{
    public class CanvassHeaderDto
    {
        public int CanvassId { get; set; }
        public string CanvassNo { get; set; } = string.Empty;
        public int MprfId { get; set; }
        public string MprfNo { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CanvassDate { get; set; }
    }
}
