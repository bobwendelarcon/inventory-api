using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Manufacturing.Materials
{
    [Table("material_inventory_planning")]
    public class MaterialInventoryPlanning
    {
        [Key]
        [Column("planning_id")]
        public int planning_id { get; set; }

        [Column("material_id")]
        public int material_id { get; set; }

        [Column("branch_id")]
        public int branch_id { get; set; }

        [Column("safety_stock_qty")]
        public decimal safety_stock_qty { get; set; } = 0;

        [Column("reorder_point_qty")]
        public decimal reorder_point_qty { get; set; } = 0;

        [Column("minimum_order_qty")]
        public decimal minimum_order_qty { get; set; } = 0;

        [Column("preferred_supplier_id")]
        public int? preferred_supplier_id { get; set; }

        [Column("auto_request_enabled")]
        public bool auto_request_enabled { get; set; } = false;

        [Column("is_active")]
        public bool is_active { get; set; } = true;

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }

        [ForeignKey("material_id")]
        public Material? Material { get; set; }
    }
}