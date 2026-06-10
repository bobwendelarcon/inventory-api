using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Canvassing
{
    [Table("purchasing_canvass_line")]
    public class PurchasingCanvassLine
    {
        [Key]
        [Column("canvass_line_id")]
        public int CanvassLineId { get; set; }

        [Column("canvass_id")]
        public int CanvassId { get; set; }

        [Column("mprf_line_id")]
        public int MprfLineId { get; set; }

        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("purchasing_qty")]
        public decimal PurchasingQty { get; set; }

        [Column("uom")]
        public string? Uom { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(CanvassId))]
        public PurchasingCanvassHeader? Header { get; set; }

        public ICollection<PurchasingCanvassQuote> Quotes { get; set; } = new List<PurchasingCanvassQuote>();
    }
}