namespace inventory_api.DTOs.Purchasing.ReceivingReports
{
    public class CreateReceivingReportDto
    {
        public int PoId { get; set; }
        public string? SiDrNo { get; set; }
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