namespace inventory_api.DTOs
{
    public class CompleteChecklistCustomerDto
    {
        public long checklist_id { get; set; }
        public string customer_name { get; set; } = "";

        public string? adjusted_by { get; set; }
        public string? dr_no { get; set; }
        public string? inv_no { get; set; }
        public string? po_no { get; set; }
        public string? remarks { get; set; }
    }
}