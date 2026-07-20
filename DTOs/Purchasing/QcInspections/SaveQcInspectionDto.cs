namespace inventory_api.DTOs.Purchasing.QcInspections
{
    public class SaveQcInspectionDto
    {
        public DateTime? InspectionDate { get; set; }

        public string? Remarks { get; set; }

        public string? InspectorId { get; set; }

        public List<SaveQcInspectionLineDto> Lines { get; set; } = new();
    }

    public class SaveQcInspectionLineDto
    {
        public int QcLineId { get; set; }

        public string? Remarks { get; set; }

        public List<SaveQcInspectionLineLotDto> Lots { get; set; } = new();
    }

    public class SaveQcInspectionLineLotDto
    {
        public int? QcLineLotId { get; set; }

        public string LotNo { get; set; } = string.Empty;

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public decimal ReceivedQty { get; set; }

        public decimal AcceptedQty { get; set; }

        public decimal RejectedQty { get; set; }

        public string? RejectionReason { get; set; }

        public string? Remarks { get; set; }
    }
}