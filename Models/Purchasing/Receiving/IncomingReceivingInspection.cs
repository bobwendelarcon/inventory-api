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


        // PO / Supplier checks

        [Column("po_matched")]
        public bool PoMatched { get; set; }

        [Column("delivery_scheduled")]
        public bool DeliveryScheduled { get; set; }

        [Column("approved_supplier")]
        public bool ApprovedSupplier { get; set; }


        // Document checks

        [Column("sales_invoice_available")]
        public bool SalesInvoiceAvailable { get; set; }

        [Column("delivery_receipt_available")]
        public bool DeliveryReceiptAvailable { get; set; }

        [Column("coa_available")]
        public bool CoaAvailable { get; set; }


        // Vehicle checks

        [Column("vehicle_clean")]
        public bool VehicleClean { get; set; }

        [Column("vehicle_dry")]
        public bool VehicleDry { get; set; }

        [Column("vehicle_odor_free")]
        public bool VehicleOdorFree { get; set; }

        [Column("vehicle_residue_free")]
        public bool VehicleResidueFree { get; set; }


        // Material checks

        [Column("material_clean")]
        public bool MaterialClean { get; set; }

        [Column("material_covered_or_sealed")]
        public bool MaterialCoveredOrSealed { get; set; }


        // Inspection information

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