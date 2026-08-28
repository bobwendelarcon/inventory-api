namespace inventory_api.DTOs.Purchasing.IncomingReceiving
{
    public class IncomingReceivingInspectionDto
    {
        // PO / Supplier
        public bool PoMatched { get; set; }

        public bool DeliveryScheduled { get; set; }

        public bool ApprovedSupplier { get; set; }


        // Documents
        public bool SalesInvoiceAvailable { get; set; }

        public bool DeliveryReceiptAvailable { get; set; }

        public bool CoaAvailable { get; set; }


        // Vehicle
        public bool VehicleClean { get; set; }

        public bool VehicleDry { get; set; }

        public bool VehicleOdorFree { get; set; }

        public bool VehicleResidueFree { get; set; }


        // Material
        public bool MaterialClean { get; set; }

        public bool MaterialCoveredOrSealed { get; set; }


        public string? Remarks { get; set; }
    }
}