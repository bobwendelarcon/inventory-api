namespace inventory_api.DTOs.Purchasing.IncomingReceiving
{
    public class IncomingReceivingLineDto
    {
        public int PoLineId { get; set; }

        public decimal DeliveredQty { get; set; }

        public bool PackagingOk { get; set; } = true;

        public bool ContaminationOk { get; set; } = true;

        public bool LabelingOk { get; set; } = true;

        public string? Remarks { get; set; }
    }
}