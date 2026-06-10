namespace inventory_api.DTOs.Purchasing
{
    public class ReviewMprfDto
    {
        public string? reviewed_by { get; set; }
        public string? review_decision { get; set; }
        public string? review_remarks { get; set; }
        public List<ReviewMprfLineDto> lines { get; set; } = new();
    }

    public class ReviewMprfLineDto
    {
        public int mprf_line_id { get; set; }
        public decimal purchasing_qty { get; set; }
        public string? purchasing_remarks { get; set; }
        public string? item_decision { get; set; }
    }
}