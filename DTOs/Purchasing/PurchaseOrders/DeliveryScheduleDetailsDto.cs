namespace inventory_api.DTOs.Purchasing.PurchaseOrders
{
    public class DeliveryScheduleDetailsDto
    {
        public int ScheduleId { get; set; }
        public int ScheduleNo { get; set; }

        public int PoId { get; set; }
        public string PoNo { get; set; } = string.Empty;
        public string? PrintedPoNo { get; set; }

        public DateTime ScheduledDate { get; set; }

        public string ScheduleStatus { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public int? RescheduledFromScheduleId { get; set; }

        public string? Remarks { get; set; }

        public decimal TotalScheduledQty { get; set; }
        public decimal TotalReceivedQty { get; set; }
        public decimal TotalRemainingQty { get; set; }

        public int? RrId { get; set; }
        public string? RrNo { get; set; }
        public string? RrStatus { get; set; }

        public string? QcNo { get; set; }
        public string? QcStatus { get; set; }
        public string? QcDecision { get; set; }

        public bool CanReschedule { get; set; }

        public List<DeliveryScheduleLineDetailsDto> Lines { get; set; } = new();
    }

    public class DeliveryScheduleLineDetailsDto
    {
        public int ScheduleLineId { get; set; }

        public int PoLineId { get; set; }
        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public decimal PoQty { get; set; }

        public decimal ScheduledQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RemainingQty { get; set; }

        public string Uom { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}