namespace inventory_api.DTOs.Purchasing.Canvassing
{
    public class CanvassLineDto
    {
        public int CanvassLineId { get; set; }
        public int MprfLineId { get; set; }
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
        public decimal PurchasingQty { get; set; }
        public string Uom { get; set; } = string.Empty;
        public List<CanvassQuoteDto> Quotes { get; set; } = new();
    }
}
