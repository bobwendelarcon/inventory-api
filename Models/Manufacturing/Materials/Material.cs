using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Manufacturing.Materials
{
    [Table("materials")]
    public class Material
    {
        [Key]
        [Column("material_id")]
        public int material_id { get; set; }

        [Required]
        [Column("material_code")]
        public string material_code { get; set; } = string.Empty;

        [Required]
        [Column("material_name")]
        public string material_name { get; set; } = string.Empty;



        [Column("material_category_id")]
        public int? material_category_id { get; set; }

        [Column("material_subcategory_id")]
        public int? material_subcategory_id { get; set; }

        [ForeignKey("material_category_id")]
        public MaterialCategory? Category { get; set; }

        [ForeignKey("material_subcategory_id")]
        public MaterialSubCategory? SubCategory { get; set; }

        [Required]
        [Column("uom")]
        public string uom { get; set; } = string.Empty;

        [Column("pack_uom")]
        public string? pack_uom { get; set; }

        [Column("pack_qty")]
        public decimal? pack_qty { get; set; }

        [Column("description")]
        public string? description { get; set; }

        [Column("is_active")]
        public bool is_active { get; set; } = true;

        [Column("is_deleted")]
        public bool is_deleted { get; set; } = false;

        [Column("is_lot_tracked")]
        public bool is_lot_tracked { get; set; } = false;

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }

        [Column("minimum_stock")]
        public decimal minimum_stock { get; set; } = 0;


    }
}