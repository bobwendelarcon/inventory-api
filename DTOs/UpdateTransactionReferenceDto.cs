namespace inventory_api.DTOs
{
    public class UpdateTransactionReferenceDto
    {
        public long transaction_id { get; set; }
        public string? customer { get; set; }
        public string? dr_no { get; set; }
        public string? inv_no { get; set; }
        public string? po_no { get; set; }
        public string? remarks { get; set; }
    }
}