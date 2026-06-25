namespace inventory_api.DTOs
{
    public class ReplaceChecklistLotsDto
    {
        public long checklist_line_id { get; set; }
        public string? reason { get; set; }
        public List<ReplaceChecklistLotItemDto> lots { get; set; } = new();
    }

    public class ReplaceChecklistLotItemDto
    {
        public string branch_id { get; set; } = "";
        public string lot_no { get; set; } = "";
        public decimal qty { get; set; }
    }
}