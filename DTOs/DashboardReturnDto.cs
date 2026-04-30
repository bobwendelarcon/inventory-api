namespace inventory_api.DTOs
{
    public class DashboardReturnDto
    {
        public long ReturnId { get; set; }

        public string ReturnNo { get; set; } = "";
        public string CustomerName { get; set; } = "";

        public decimal Quantity { get; set; }
        public string Uom { get; set; } = "";

        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";

        public string? QuarantineLocation { get; set; }
        public DateTime? ReturnDate { get; set; }

        public string? DrNo { get; set; }
        public string? InvNo { get; set; }
        public string? PoNo { get; set; }
        public string? OrderNo { get; set; }
        public string? ChecklistNo { get; set; }
    }
}