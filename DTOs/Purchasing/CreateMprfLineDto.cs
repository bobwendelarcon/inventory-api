namespace inventory_api.DTOs.Purchasing
{
    public class CreateMprfLineDto
    {
        public int material_id { get; set; }
        public decimal qty_on_hand { get; set; }
        public decimal requested_qty { get; set; }
        public string? uom { get; set; }
        public string? remarks { get; set; }
    }
}