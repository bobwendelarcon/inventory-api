namespace inventory_api.DTOs.Purchasing.ReceivingReports
{
    public class ReceivingReportListDto
    {
        public int RrId { get; set; }
        public string RrNo { get; set; } = string.Empty;
        public string PoNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? SiDrNo { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}