namespace inventory_api.DTOs
{
    public class RemoveChecklistCustomerDto
    {
        public long checklist_id { get; set; }
        public string customer_name { get; set; } = "";
    }
}