namespace inventory_api.DTOs.Purchasing.QaQcReceiving
{
    public class QaQcReceivingInspectionDetailsDto
    {
        public int IncomingReceivingId { get; set; }

        public string IncomingNo { get; set; } = "";

        public int PoId { get; set; }

        public string PoNo { get; set; } = "";

        public int SupplierId { get; set; }

        public string SupplierName { get; set; } = "";

        public DateTime DeliveryDate { get; set; }

        public string ReceivingStatus { get; set; } = "";

        public string? ReceivedBy { get; set; }

        public string? VerifiedBy { get; set; }

        public List<QaQcReceivingMaterialDto> Lines { get; set; }
            = new();
    }
}