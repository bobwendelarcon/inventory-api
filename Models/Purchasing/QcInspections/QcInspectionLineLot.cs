using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.QcInspections
{
    [Table("purchasing_qc_line_lot")]
    public class QcInspectionLineLot
    {
        [Key]
        [Column("qc_line_lot_id")]
        public int QcLineLotId { get; set; }

        [Column("qc_line_id")]
        public int QcLineId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("lot_no")]
        public string LotNo { get; set; } = string.Empty;

        [Column("manufacturing_date")]
        public DateTime? ManufacturingDate { get; set; }

        [Column("expiration_date")]
        public DateTime? ExpirationDate { get; set; }

        [Column("received_qty", TypeName = "decimal(18,4)")]
        public decimal ReceivedQty { get; set; }

        [Column("accepted_qty", TypeName = "decimal(18,4)")]
        public decimal AcceptedQty { get; set; }

        [Column("rejected_qty", TypeName = "decimal(18,4)")]
        public decimal RejectedQty { get; set; }

        [MaxLength(255)]
        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        [Column("remarks")]
        public string? Remarks { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = "PENDING";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(50)]
        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(50)]
        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        [ForeignKey(nameof(QcLineId))]
        public QcInspectionLine QcLine { get; set; } = null!;
    }
}