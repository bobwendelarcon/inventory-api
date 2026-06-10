using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Canvassing
{
    [Table("purchasing_canvass_header")]
    public class PurchasingCanvassHeader
    {
        [Key]
        [Column("canvass_id")]
        public int CanvassId { get; set; }

        [Column("canvass_no")]
        public string CanvassNo { get; set; } = string.Empty;

        [Column("mprf_id")]
        public int MprfId { get; set; }

        [Column("canvass_date")]
        public DateTime CanvassDate { get; set; } = DateTime.Now;

        [Column("status")]
        public string Status { get; set; } = "OPEN";

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<PurchasingCanvassLine> Lines { get; set; } = new List<PurchasingCanvassLine>();
    }
}