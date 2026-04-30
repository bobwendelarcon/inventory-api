namespace inventory_api.DTOs.Reports
{
    public class ReturnReportSummaryDto
    {
        public decimal TotalReturnedQty { get; set; }
        public string TopReason { get; set; } = "";
        public string TopCustomer { get; set; } = "";
    }

    public class ReturnReportRowDto
    {
        public string ReturnNo { get; set; } = "";
        public DateTime ReturnDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string LotNo { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string Status { get; set; } = "";
        public string? LinkedTransactionNo { get; set; }
    }

    public class ReturnReportDto
    {
        public ReturnReportSummaryDto Summary { get; set; } = new();
        public List<ReturnReportRowDto> Items { get; set; } = new();
    }
}