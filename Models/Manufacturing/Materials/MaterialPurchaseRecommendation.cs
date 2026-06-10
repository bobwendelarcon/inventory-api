using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Manufacturing.Materials
{
    [Table("material_purchase_recommendations")]
    public class MaterialPurchaseRecommendation
    {
        [Key]
        [Column("recommendation_id")]
        public int recommendation_id { get; set; }

        [Column("material_id")]
        public int material_id { get; set; }

        [Column("branch_id")]
        public int branch_id { get; set; }

        [Column("current_stock")]
        public decimal current_stock { get; set; } = 0;

        [Column("reorder_point_qty")]
        public decimal reorder_point_qty { get; set; } = 0;

        [Column("suggested_qty")]
        public decimal suggested_qty { get; set; } = 0;

        [Required]
        [Column("status")]
        public string status { get; set; } = "OPEN";

        [Column("generated_date")]
        public DateTime generated_date { get; set; }

        [Column("converted_rmrf_id")]
        public int? converted_rmrf_id { get; set; }

        [Column("remarks")]
        public string? remarks { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }

        [ForeignKey("material_id")]
        public Material? Material { get; set; }
    }
}