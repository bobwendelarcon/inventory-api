using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Manufacturing.Materials
{
    [Table("material_subcategories")]
    public class MaterialSubCategory
    {
        [Key]
        [Column("material_subcategory_id")]
        public int material_subcategory_id { get; set; }

        [Required]
        [Column("material_category_id")]
        public int material_category_id { get; set; }

        [Required]
        [Column("subcategory_name")]
        public string subcategory_name { get; set; } = string.Empty;

        [Column("description")]
        public string? description { get; set; }

        [Column("is_active")]
        public bool is_active { get; set; } = true;

        [Column("is_deleted")]
        public bool is_deleted { get; set; } = false;

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }

        [ForeignKey("material_category_id")]
        public MaterialCategory? Category { get; set; }
    }
}