// Models/ProductToProduceLine.cs
namespace inventory_api.Models
{
    public class ProductToProduceLine
    {
        public long ptp_line_id { get; set; }
        public long ptp_id { get; set; }

        public string product_id { get; set; } = "";
        public string product_name { get; set; } = "";

        public decimal suggested_qty { get; set; }

        public decimal qty_input { get; set; }
        public string uom_type { get; set; } = "BASE";

        public decimal requested_qty { get; set; }

        public string? uom { get; set; }
        public string? pack_uom { get; set; }
        public decimal? pack_qty { get; set; }

        public DateTime? delivery_date { get; set; }
        public string? remarks { get; set; }

        public string source_type { get; set; } = "MANUAL";
        public string status { get; set; } = "PENDING";

        public ProductToProduceHeader? Header { get; set; }
    }
}