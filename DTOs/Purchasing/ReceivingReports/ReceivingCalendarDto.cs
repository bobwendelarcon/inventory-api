namespace inventory_api.DTOs.Purchasing.ReceivingReports
{
    public class ReceivingCalendarDto
    {
        public int ScheduleId { get; set; }

        public int ScheduleNo { get; set; }

        public int PoId { get; set; }

        public string? PrintedPoNo { get; set; }

        public string PoNo { get; set; } = string.Empty;

        public DateTime DeliveryDate { get; set; }

        public int SupplierId { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public int MaterialCount { get; set; }

        public decimal TotalPoQty { get; set; }

        public decimal TotalReceivedQty { get; set; }

        public decimal TotalBalanceQty { get; set; }
    }
}