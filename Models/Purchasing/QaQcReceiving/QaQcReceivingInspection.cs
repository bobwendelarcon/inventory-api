using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.QaQcReceiving
{
    [Table("purchasing_qa_qc_receiving_inspection")]
    public class QaQcReceivingInspection
    {
        [Key]
        [Column("qa_receiving_id")]
        public int QaReceivingId { get; set; }

        [Column("inspection_no")]
        [MaxLength(30)]
        public string InspectionNo { get; set; } = "";

        [Column("incoming_receiving_id")]
        public int IncomingReceivingId { get; set; }

        [Column("po_id")]
        public int PoId { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("inspection_date")]
        public DateTime InspectionDate { get; set; }

        [Column("status")]
        [MaxLength(40)]
        public string Status { get; set; } = "FOR_INSPECTION";

        [Column("remarks")]
        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Column("inspected_by")]
        [MaxLength(100)]
        public string? InspectedBy { get; set; }

        [Column("received_by")]
        [MaxLength(100)]
        public string? ReceivedBy { get; set; }

        [Column("noted_by")]
        [MaxLength(100)]
        public string? NotedBy { get; set; }

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
        public ICollection<QaQcReceivingInspectionLine> Lines { get; set; }
            = new List<QaQcReceivingInspectionLine>();
    }
}