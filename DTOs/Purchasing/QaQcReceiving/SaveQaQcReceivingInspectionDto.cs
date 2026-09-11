namespace inventory_api.DTOs.Purchasing.QaQcReceiving
{
    public class SaveQaQcReceivingInspectionDto
    {
        public int IncomingReceivingId { get; set; }

        public DateTime InspectionDate { get; set; }

        public string? Remarks { get; set; }

        public string? ReceivedBy { get; set; }

        public string? NotedBy { get; set; }

        public List<SaveQaQcReceivingInspectionLineDto> Lines { get; set; }
            = new();
    }
}