using System;
using System.Collections.Generic;

namespace inventory_api.DTOs.Purchasing.IncomingReceiving
{
    public class IncomingReceivingDetailsDto
    {
        public int IncomingReceivingId { get; set; }

        public string IncomingNo { get; set; } = "";

        public int PoId { get; set; }

        public string PoNo { get; set; } = "";

        public string? PrintedPoNo { get; set; }

        public int ScheduleId { get; set; }

        public string ScheduleNo { get; set; } = "";

        public int SupplierId { get; set; }

        public string SupplierName { get; set; } = "";

        public string? BranchId { get; set; }

        public DateTime DeliveryDate { get; set; }

        public string? SiDrNo { get; set; }

        public string ReceivingStatus { get; set; } = "";

        public bool DocumentsComplete { get; set; }

        public string? MissingDocuments { get; set; }

        public string? ReceivingRemarks { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public IncomingReceivingInspectionDetailsDto? Inspection { get; set; }

        public List<IncomingReceivingLineDetailsDto> Lines { get; set; }
            = new();

        public List<ReceivingTrackingHistoryDto> TrackingHistory { get; set; }
            = new();
    }


    public class IncomingReceivingLineDetailsDto
    {
        public int IncomingReceivingLineId { get; set; }

        public int PoLineId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = "";

        public string MaterialName { get; set; } = "";

        public decimal ScheduledQty { get; set; }

        public decimal DeliveredQty { get; set; }

        public string? Uom { get; set; }

        public bool PackagingOk { get; set; }

        public bool ContaminationOk { get; set; }

        public bool LabelingOk { get; set; }

        public string? Remarks { get; set; }
    }


    public class IncomingReceivingInspectionDetailsDto
    {
        public bool PoMatched { get; set; }

        public bool DeliveryScheduled { get; set; }

        public bool ApprovedSupplier { get; set; }

        public bool SalesInvoiceAvailable { get; set; }

        public bool DeliveryReceiptAvailable { get; set; }

        public bool CoaAvailable { get; set; }

        public bool VehicleClean { get; set; }

        public bool VehicleDry { get; set; }

        public bool VehicleOdorFree { get; set; }

        public bool VehicleResidueFree { get; set; }

        public bool MaterialClean { get; set; }

        public bool MaterialCoveredOrSealed { get; set; }

        public string? Remarks { get; set; }

        public string? CheckedBy { get; set; }

        public DateTime? CheckedAt { get; set; }
    }


    public class ReceivingTrackingHistoryDto
    {
        public long TrackingId { get; set; }

        public string EventCode { get; set; } = "";

        public string? Status { get; set; }

        public string? EventDescription { get; set; }

        public string? PerformedBy { get; set; }

        public string? PerformedByRole { get; set; }

        public DateTime EventAt { get; set; }

        public string? Remarks { get; set; }
    }
}