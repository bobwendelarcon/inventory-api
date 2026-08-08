using inventory_api.Models.Purchasing.PurchaseOrders;

namespace inventory_api.Models.Purchasing.ReceivingReports
{
    public class ReceivingReportHeader
    {
        public int RrId { get; set; }
        public string RrNo { get; set; } = string.Empty;

        public int PoId { get; set; }
        public int? ScheduleId { get; set; }

      
        public string PoNo { get; set; } = string.Empty;
        public int SupplierId { get; set; }

        public string? SiDrNo { get; set; }
        public DateTime DeliveryDate { get; set; }

        public string? Remarks { get; set; }

        public string Status { get; set; } = "DRAFT";

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? QcBy { get; set; }
        public DateTime? QcAt { get; set; }

        public string? CommittedBy { get; set; }
        public DateTime? CommittedAt { get; set; }

        public List<ReceivingReportLine> Lines { get; set; } = new();
        public PurchaseOrderDeliverySchedule? DeliverySchedule { get; set; }
    }
}