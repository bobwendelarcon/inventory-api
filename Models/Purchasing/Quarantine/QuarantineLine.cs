using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Quarantine
{
    [Table("purchasing_quarantine_line")]
    public class QuarantineLine
    {
        [Key]
        [Column("quarantine_line_id")]
        public int QuarantineLineId { get; set; }

        [Column("quarantine_id")]
        public int QuarantineId { get; set; }

        [Column("qa_receiving_line_id")]
        public int QaReceivingLineId { get; set; }

        [Column("incoming_receiving_line_lot_id")]
        public int? IncomingReceivingLineLotId { get; set; }

        [Column("qc_line_id")]
        public int? QcLineId { get; set; }

        [Column("qc_line_lot_id")]
        public int? QcLineLotId { get; set; }


        [Column("incoming_receiving_line_id")]
        public int? IncomingReceivingLineId { get; set; }

        [Column("po_line_id")]
        public int PoLineId { get; set; }

        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("lot_no")]
        public string? LotNo { get; set; }

        [Column("manufacturing_date")]
        public DateTime? ManufacturingDate { get; set; }

        [Column("expiration_date")]
        public DateTime? ExpirationDate { get; set; }

        [Column("qc_received_qty")]
        public decimal QcReceivedQty { get; set; }

        [Column("qc_accepted_qty")]
        public decimal QcAcceptedQty { get; set; }

        [Column("qc_rejected_qty")]
        public decimal QcRejectedQty { get; set; }

        [Column("status")]
        public string Status { get; set; } = "ON_HOLD";

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }


        // Navigation
        [ForeignKey(nameof(QuarantineId))]
        public QuarantineHeader? Header { get; set; }
    }
}