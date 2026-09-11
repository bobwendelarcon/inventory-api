namespace inventory_api.DTOs.Purchasing.IncomingReceiving
{
    public class IncomingReceivingInspectionDto
    {
        // ============================================================
        // PO / SUPPLIER VALIDATION
        // ============================================================

        public bool PoMatched { get; set; }

        public bool DeliveryScheduled { get; set; }

        public bool ApprovedSupplier { get; set; }


        // ============================================================
        // DOCUMENTS
        // ============================================================

        public bool SalesInvoiceAvailable { get; set; }

        public bool DeliveryReceiptAvailable { get; set; }

        public bool CoaAvailable { get; set; }


        // ============================================================
        // MATERIAL RECEIVING FORM CHECKLIST
        // ============================================================

        // Correct quantity was delivered
        public bool CorrectQuantityDelivered { get; set; }

        // Truck / vehicle security
        public bool TruckDoorLockInPlace { get; set; }

        // Vehicle cleanliness
        public bool VehicleClean { get; set; }

        // Bags / drums / boxes / containers are clean and sealed
        public bool ContainersCleanAndSealed { get; set; }

        // No visible contamination / spoilage
        public bool NoVisibleContaminationOrSpoilage { get; set; }

        // Labels are present and readable
        public bool LabelsPresentAndLegible { get; set; }

        // Driver identity was verified
        public bool DriverIdentityVerified { get; set; }

        // No unauthorized access during unloading
        public bool NoUnauthorizedAccessDuringUnloading { get; set; }

        // Vehicle checked for hidden compartments
        public bool HiddenCompartmentChecked { get; set; }

        // Receiving happened only in authorized receiving area
        public bool ReceivedInAuthorizedZone { get; set; }


        // ============================================================
        // REMARKS
        // ============================================================

        public string? Remarks { get; set; }
    }
}