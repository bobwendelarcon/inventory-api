using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.QcInspections
{
    public class QcInspectionHeader
    {
        public int QcId { get; set; }
        public string QcNo { get; set; } = string.Empty;

        public int? RrId { get; set; }
        public string? RrNo { get; set; }

        public int PoId { get; set; }
        public string PoNo { get; set; } = string.Empty;

        public int SupplierId { get; set; }

        [Column("incoming_receiving_id")]
        public int? IncomingReceivingId { get; set; }

        [Column("incoming_no")]
        [MaxLength(30)]
        public string? IncomingNo { get; set; }


        public DateTime? InspectionDate { get; set; }
        public string? InspectorId { get; set; }

        public string Status { get; set; } = "FOR_INSPECTION";
        public string? Decision { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? CommittedBy { get; set; }
        public DateTime? CommittedAt { get; set; }

        public List<QcInspectionLine> Lines { get; set; } = new();
    }
}