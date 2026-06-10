using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing
{
    [Table("purchasing_mprf_header")]
    public class MprfHeader
    {
        [Key]
        public int mprf_id { get; set; }

        public string? mprf_no { get; set; }
        public string? category { get; set; }
        public DateTime request_date { get; set; }
        public string? week { get; set; }
        //public DateTime? needed_date { get; set; }
        public string? requested_by { get; set; }
        public string? reviewed_by { get; set; }
        public string status { get; set; } = "DRAFT";
        //public string? remarks { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }

        public string? review_decision { get; set; }
        public string? review_remarks { get; set; }
        public DateTime? reviewed_at { get; set; }


        public List<MprfLine> lines { get; set; } = new();
    }
}