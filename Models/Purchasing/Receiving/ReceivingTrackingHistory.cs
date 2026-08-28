using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Receiving
{
    [Table("purchasing_receiving_tracking_history")]
    public class ReceivingTrackingHistory
    {
        [Key]
        [Column("tracking_id")]
        public long TrackingId { get; set; }

        [Column("po_id")]
        public int PoId { get; set; }

        [Column("schedule_id")]
        public int? ScheduleId { get; set; }

        [Column("incoming_receiving_id")]
        public int? IncomingReceivingId { get; set; }

        [Column("event_code")]
        [MaxLength(50)]
        public string EventCode { get; set; } = "";

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; }

        [Column("event_description")]
        [MaxLength(500)]
        public string? EventDescription { get; set; }

        [Column("performed_by")]
        [MaxLength(100)]
        public string? PerformedBy { get; set; }

        [Column("performed_by_role")]
        [MaxLength(100)]
        public string? PerformedByRole { get; set; }

        [Column("event_at")]
        public DateTime EventAt { get; set; }

        [Column("remarks")]
        [MaxLength(1000)]
        public string? Remarks { get; set; }


        // Navigation

        [ForeignKey(nameof(IncomingReceivingId))]
        public IncomingReceiving? IncomingReceiving { get; set; }
    }
}