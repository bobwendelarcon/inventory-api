namespace inventory_api.DTOs.Reports.TimeInMotion
{
    public class TimeInMotionStageDto
    {
        public string Stage { get; set; } = string.Empty;

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public double? DurationMinutes { get; set; }

        public string? ResponsibleUserId { get; set; }

        public string? ResponsibleUserName { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}