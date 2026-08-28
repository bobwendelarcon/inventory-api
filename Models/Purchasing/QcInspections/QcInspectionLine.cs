using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.QcInspections
{
    public class QcInspectionLine
    {
        public int QcLineId { get; set; }
        public int QcId { get; set; }

        [Column("rr_line_id")]
        public int? RrLineId { get; set; }

        public int PoLineId { get; set; }
        public int MaterialId { get; set; }

        [Column("incoming_receiving_line_id")]
        public int? IncomingReceivingLineId { get; set; }

        public decimal ReceivedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }

        public string? Remarks { get; set; }

        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public QcInspectionHeader? Header { get; set; }

        public ICollection<QcInspectionLineLot> Lots { get; set; }
            = new List<QcInspectionLineLot>();
    }
}