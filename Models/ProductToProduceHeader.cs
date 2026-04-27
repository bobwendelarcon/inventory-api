// Models/ProductToProduceHeader.cs
namespace inventory_api.Models
{
    public class ProductToProduceHeader
    {
        public long ptp_id { get; set; }
        public string ptp_no { get; set; } = "";
        public DateTime requested_date { get; set; }
        public string status { get; set; } = "PENDING";
        public string? remarks { get; set; }
        public string? created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }

        public List<ProductToProduceLine> Lines { get; set; } = new();
    }
}