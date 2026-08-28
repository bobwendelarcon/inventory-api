using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Quarantine
{
    [Table("purchasing_quarantine_header")]
    public class QuarantineHeader
    {
        [Key]
        [Column("quarantine_id")]
        public int QuarantineId { get; set; }

        [Column("quarantine_no")]
        public string QuarantineNo { get; set; } = string.Empty;

        [Column("incoming_receiving_id")]
        public int IncomingReceivingId { get; set; }

        [Column("qc_id")]
        public int QcId { get; set; }

        [Column("po_id")]
        public int PoId { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("status")]
        public string Status { get; set; } = "ON_HOLD";

        [Column("decision")]
        public string? Decision { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("released_by")]
        public string? ReleasedBy { get; set; }

        [Column("released_at")]
        public DateTime? ReleasedAt { get; set; }

        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }


        // Navigation
        public ICollection<QuarantineLine> Lines { get; set; }
            = new List<QuarantineLine>();
    }
}