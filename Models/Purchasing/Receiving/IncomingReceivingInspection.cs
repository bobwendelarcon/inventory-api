using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Receiving
{
    [Table("purchasing_incoming_receiving_inspection")]
    public class IncomingReceivingInspection
    {
        [Key]
        [Column("inspection_id")]
        public int InspectionId { get; set; }

        [Column("incoming_receiving_id")]
        public int IncomingReceivingId { get; set; }


        // ============================================================
        // PO / SUPPLIER VALIDATION
        // ============================================================

        [Column("po_matched")]
        public bool PoMatched { get; set; }

        [Column("delivery_scheduled")]
        public bool DeliveryScheduled { get; set; }

        [Column("approved_supplier")]
        public bool ApprovedSupplier { get; set; }


        // ============================================================
        // DOCUMENTS
        // ============================================================

        [Column("sales_invoice_available")]
        public bool SalesInvoiceAvailable { get; set; }

        [Column("delivery_receipt_available")]
        public bool DeliveryReceiptAvailable { get; set; }

        [Column("coa_available")]
        public bool CoaAvailable { get; set; }


        // ============================================================
        // MATERIAL RECEIVING FORM
        // ============================================================

        [Column("correct_quantity_delivered")]
        public bool CorrectQuantityDelivered { get; set; }

        [Column("truck_door_lock_in_place")]
        public bool TruckDoorLockInPlace { get; set; }

        [Column("vehicle_clean")]
        public bool VehicleClean { get; set; }

        [Column("containers_clean_and_sealed")]
        public bool ContainersCleanAndSealed { get; set; }

        [Column("no_visible_contamination_or_spoilage")]
        public bool NoVisibleContaminationOrSpoilage { get; set; }

        [Column("labels_present_and_legible")]
        public bool LabelsPresentAndLegible { get; set; }

        [Column("driver_identity_verified")]
        public bool DriverIdentityVerified { get; set; }

        [Column("no_unauthorized_access_during_unloading")]
        public bool NoUnauthorizedAccessDuringUnloading { get; set; }

        [Column("hidden_compartment_checked")]
        public bool HiddenCompartmentChecked { get; set; }

        [Column("received_in_authorized_zone")]
        public bool ReceivedInAuthorizedZone { get; set; }


        // ============================================================
        // INSPECTION INFORMATION
        // ============================================================

        [Column("remarks")]
        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Column("checked_by")]
        [MaxLength(100)]
        public string? CheckedBy { get; set; }

        [Column("checked_at")]
        public DateTime? CheckedAt { get; set; }


        // Navigation

        [ForeignKey(nameof(IncomingReceivingId))]
        public IncomingReceiving? IncomingReceiving { get; set; }
    }
}