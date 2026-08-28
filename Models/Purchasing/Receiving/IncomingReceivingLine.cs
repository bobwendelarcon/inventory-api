using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Receiving
{
    [Table("purchasing_incoming_receiving_line")]
    public class IncomingReceivingLine
    {
        [Key]
        [Column("incoming_receiving_line_id")]
        public int IncomingReceivingLineId { get; set; }

        [Column("incoming_receiving_id")]
        public int IncomingReceivingId { get; set; }

        [Column("po_line_id")]
        public int PoLineId { get; set; }

        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("scheduled_qty")]
        public decimal ScheduledQty { get; set; }

        [Column("delivered_qty")]
        public decimal DeliveredQty { get; set; }

        [Column("uom")]
        [MaxLength(50)]
        public string? Uom { get; set; }

        [Column("packaging_ok")]
        public bool PackagingOk { get; set; } = true;

        [Column("contamination_ok")]
        public bool ContaminationOk { get; set; } = true;

        [Column("labeling_ok")]
        public bool LabelingOk { get; set; } = true;

        [Column("remarks")]
        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }


        // Navigation
        [ForeignKey(nameof(IncomingReceivingId))]
        public IncomingReceiving? IncomingReceiving { get; set; }
    }
}