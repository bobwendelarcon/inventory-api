namespace inventory_api.DTOs
{
    public class UpdateChecklistLineLotDto
    {
        public long checklist_line_id { get; set; }
        public string lot_no { get; set; } = "";
        public string branch_id { get; set; } = "";
    }
}
