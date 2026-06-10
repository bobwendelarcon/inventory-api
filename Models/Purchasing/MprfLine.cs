using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing
{
    [Table("purchasing_mprf_line")]
    public class MprfLine
    {
        [Key]
        public int mprf_line_id { get; set; }

        public int mprf_id { get; set; }
        public int material_id { get; set; }

        [NotMapped]
        public string? material_code { get; set; }

        [NotMapped]
        public string? material_name { get; set; }

        public decimal qty_on_hand { get; set; }
        public decimal requested_qty { get; set; }
        public string? uom { get; set; }
        public string? remarks { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public decimal? purchasing_qty { get; set; }
        public string? purchasing_remarks { get; set; }

        public string? item_decision { get; set; }
    }
}