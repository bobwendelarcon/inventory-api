namespace inventory_api.DTOs
{
    public class ProduceStockDto
    {
        public long ptpLineId { get; set; }
        public decimal quantity { get; set; }

        public string branchId { get; set; } = "";
        public string lotNo { get; set; } = "";
        public string transmittalNo { get; set; } = "";

        public DateTime? manufacturingDate { get; set; }
        public DateTime? expirationDate { get; set; }

        public string? producedBy { get; set; }
    }
}
