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
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public string? RejectionReason { get; set; }
        public string? Remarks { get; set; }
    }
}