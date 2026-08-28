namespace inventory_api.DTOs.Reports.TimeInMotion
{
    public class TimeInMotionListDto
    {
        public int IncomingReceivingId { get; set; }

        public string IncomingNo { get; set; } = string.Empty;

        public string PoNo { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public string BranchId { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public int? QcId { get; set; }

        public string? QcNo { get; set; }

        public int? QuarantineId { get; set; }

        public string? QuarantineNo { get; set; }

        public int? ProcessingId { get; set; }

        public string? ProcessingNo { get; set; }

        public int? FinalRrId { get; set; }

        public string? FinalRrNo { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public double? TotalDurationMinutes { get; set; }

        public string CurrentStage { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public List<TimeInMotionStageDto> Stages { get; set; }
            = new();
    }
}