namespace inventory_api.DTOs.Purchasing.ReceivingReports
{
    public class CompleteFinalRrDto
    {
        public string? Remarks { get; set; }
    }

    public class CompleteFinalRrResultDto
    {
        public int RrId { get; set; }
        public string RrNo { get; set; } = string.Empty;
    }
}