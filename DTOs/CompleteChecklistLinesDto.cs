namespace inventory_api.DTOs
{
    public class CompleteChecklistLinesDto
    {
        public long checklist_id { get; set; }

        public List<long> checklist_line_ids { get; set; } = new();

        public string? adjusted_by { get; set; }
        public string? dr_no { get; set; }
        public string? inv_no { get; set; }
        public string? po_no { get; set; }
        public string? remarks { get; set; }
    }
}