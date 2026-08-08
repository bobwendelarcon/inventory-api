using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Manufacturing.Materials
{
    [Table("material_inventory_transactions")]
    public class MaterialInventoryTransaction
    {
        [Key]
        [Column("transaction_id")]
        public int transaction_id { get; set; }

        [Column("material_id")]
        public int material_id { get; set; }

        [Column("branch_id")]
        public int branch_id { get; set; }

        [Column("lot_no")]
        public string? lot_no { get; set; }

        [Required]
        [Column("transaction_type")]
        public string transaction_type { get; set; } = string.Empty;

        [Column("quantity")]
        public decimal quantity { get; set; }

        [Required]
        [Column("uom")]
        public string uom { get; set; } = string.Empty;

        [Column("reference_type")]
        public string? reference_type { get; set; }

        [Column("reference_id")]
        public int? reference_id { get; set; }

        [Column("reference_no")]
        public string? reference_no { get; set; }

        [Column("remarks")]
        public string? remarks { get; set; }

        [Column("encoded_by")]
        public int? encoded_by { get; set; }

        [Column("transaction_date")]
        public DateTime transaction_date { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [ForeignKey("material_id")]
        public Material? Material { get; set; }
    }
}