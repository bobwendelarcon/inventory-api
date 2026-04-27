namespace inventory_api.DTOs
{
    public class PlanningShortageDto
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal ShortageQty { get; set; }
        public string Uom { get; set; } = "";

        public string? PackUom { get; set; }
        public decimal? PackQty { get; set; }
    }

    public class CreateProductToProduceDto
    {
        public DateTime RequestedDate { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }

        public List<CreateProductToProduceLineDto> Lines { get; set; } = new();
    }

    public class CreateProductToProduceLineDto
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";

        public decimal SuggestedQty { get; set; }

        public decimal QtyInput { get; set; }
        public string UomType { get; set; } = "BASE";

        // This is the final BASE qty
        public decimal RequestedQty { get; set; }

        public string? Uom { get; set; }
        public string? PackUom { get; set; }
        public decimal? PackQty { get; set; }

        public DateTime? DeliveryDate { get; set; }
        public string? Remarks { get; set; }

        public string SourceType { get; set; } = "MANUAL";
    }
}