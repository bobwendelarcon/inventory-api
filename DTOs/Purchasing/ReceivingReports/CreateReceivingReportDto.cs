namespace inventory_api.DTOs.Purchasing.ReceivingReports
{
    public class CreateReceivingReportDto
    {
        public int ScheduleId { get; set; }

        public string? SiDrNo { get; set; }

        // Actual truck arrival date
        public DateTime DeliveryDate { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public List<CreateReceivingReportLineDto> Lines { get; set; } = new();
    }

    public class CreateReceivingReportLineDto
    {
        public int PoLineId { get; set; }

        public decimal ReceiveQty { get; set; }

        public string? Remarks { get; set; }
    }
}