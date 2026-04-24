namespace inventory_api.DTOs
{
    public class DashboardTransactionDto
    {
        public DateTime TransactionDate { get; set; }

        public string ReferenceNo { get; set; } = "";


        // CHK-xxx / STK-IN-xxx / RET-xxx


        public string CustomerName { get; set; } = "";
        public string DrNo { get; set; } = "";
        public string InvNo { get; set; } = "";
        public string PoNo { get; set; } = "";
        public string OrderNo { get; set; } = "";
        public string ChecklistNo { get; set; } = "";

        public string LotNo { get; set; } = "";
        public string ProductName { get; set; } = "";

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "";

        public string Type { get; set; } = "";
        // IN / OUT

        public string? Remarks { get; set; }
    }
}