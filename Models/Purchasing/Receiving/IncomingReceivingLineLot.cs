using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Receiving
{
    [Table("purchasing_incoming_receiving_line_lot")]
    public class IncomingReceivingLineLot
    {
        [Key]
        [Column("incoming_receiving_line_lot_id")]
        public int IncomingReceivingLineLotId { get; set; }

        [Column("incoming_receiving_line_id")]
        public int IncomingReceivingLineId { get; set; }

        [Column("manufacturer_id")]
        public int? ManufacturerId { get; set; }

        [Column("lot_no")]
        [MaxLength(100)]
        public string? LotNo { get; set; }

        [Column("manufacturing_date")]
        public DateTime? ManufacturingDate { get; set; }

        [Column("expiration_date")]
        public DateTime? ExpirationDate { get; set; }

        [Column("item_count")]
        public decimal? ItemCount { get; set; }

        [Column("weight")]
        public decimal? Weight { get; set; }

        [Column("remarks")]
        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }


        // Navigation

        [ForeignKey(nameof(IncomingReceivingLineId))]
        public IncomingReceivingLine? IncomingReceivingLine { get; set; }
    }
}