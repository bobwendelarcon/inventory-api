namespace inventory_api.DTOs
{
    public class DashboardReturnDto
    {
        public string ReturnNo { get; set; } = "";

        public string CustomerName { get; set; } = "";

        public decimal Quantity { get; set; }

        public string Reason { get; set; } = "";

        // 🔥 YOUR PROCESS FLOW
        public string Status { get; set; } = "";
        // FLOATING / QUARANTINE / FOR REPROCESS / READY FOR STOCK IN

        public string? QuarantineLocation { get; set; }

        public DateTime? ReturnDate { get; set; }
    }
}