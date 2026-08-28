using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Receiving
{
    [Table("purchasing_incoming_receiving")]
    public class IncomingReceiving
    {
        [Key]
        [Column("incoming_receiving_id")]
        public int IncomingReceivingId { get; set; }

        [Column("incoming_no")]
        [MaxLength(30)]
        public string IncomingNo { get; set; } = "";

        [Column("po_id")]
        public int PoId { get; set; }

        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("branch_id")]
        [MaxLength(50)]
        public string? BranchId { get; set; }

        [Column("delivery_date")]
        public DateTime DeliveryDate { get; set; }

        [Column("si_dr_no")]
        [MaxLength(100)]
        public string? SiDrNo { get; set; }

        [Column("receiving_status")]
        [MaxLength(30)]
        public string ReceivingStatus { get; set; } = "CHECKING";

        [Column("documents_complete")]
        public bool DocumentsComplete { get; set; }

        [Column("missing_documents")]
        [MaxLength(500)]
        public string? MissingDocuments { get; set; }

        [Column("receiving_remarks")]
        [MaxLength(1000)]
        public string? ReceivingRemarks { get; set; }

        [Column("created_by")]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_by")]
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }


        // Navigation
        public ICollection<IncomingReceivingLine> Lines { get; set; }
            = new List<IncomingReceivingLine>();

        public IncomingReceivingInspection? Inspection { get; set; }
    }
}