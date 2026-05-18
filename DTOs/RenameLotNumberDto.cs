namespace inventory_api.DTOs
{
    public class RenameLotNumberDto
    {
        public string product_id { get; set; } = "";
        public string branch_id { get; set; } = "";
        public string old_lot_no { get; set; } = "";
        public string new_lot_no { get; set; } = "";
        public string requested_by_role { get; set; } = "";
    }
}