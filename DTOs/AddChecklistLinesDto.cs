namespace inventory_api.DTOs
{
    public class AddChecklistLinesDto
    {
        public long checklist_id { get; set; }

        public List<ChecklistLineDto> lines { get; set; }
            = new();
    }
}