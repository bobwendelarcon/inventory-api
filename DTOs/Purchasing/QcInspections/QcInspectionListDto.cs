namespace inventory_api.DTOs.Purchasing.QcInspections
{
    public class QcInspectionListDto
    {
        public int QcId { get; set; }
        public string QcNo { get; set; } = string.Empty;

        public int RrId { get; set; }
        public string RrNo { get; set; } = string.Empty;

        public string PoNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;

        public DateTime? InspectionDate { get; set; }
        public string? InspectorId { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? Decision { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}