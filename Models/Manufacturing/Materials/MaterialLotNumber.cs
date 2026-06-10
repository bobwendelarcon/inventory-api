using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Manufacturing.Materials
{
    [Table("material_lot_number")]
    public class MaterialLotNumber
    {
        [Key]
        [Column("material_lot_id")]
        public int material_lot_id { get; set; }

        [Column("material_id")]
        public int material_id { get; set; }

        [Column("branch_id")]
        public int branch_id { get; set; }

        [Required]
        [Column("lot_no")]
        public string lot_no { get; set; } = string.Empty;

        [Column("manufacturing_date")]
        public DateTime? manufacturing_date { get; set; }

        [Column("expiration_date")]
        public DateTime? expiration_date { get; set; }

        [Column("quantity")]
        public decimal quantity { get; set; } = 0;

        [Required]
        [Column("uom")]
        public string uom { get; set; } = string.Empty;

        [Column("supplier_id")]
        public int? supplier_id { get; set; }

        [Column("remarks")]
        public string? remarks { get; set; }

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