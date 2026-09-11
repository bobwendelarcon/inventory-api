using inventory_api.DTOs.Purchasing.QaQcReceiving;

public class QaQcReceivingMaterialDto
{
    public int IncomingReceivingLineId { get; set; }

    public int PoLineId { get; set; }

    public int MaterialId { get; set; }

    public string MaterialCode { get; set; } = "";

    public string MaterialName { get; set; } = "";

    public bool IsLotTracked { get; set; }

    public decimal DeliveredQty { get; set; }

    public string? Uom { get; set; }

    public decimal? TareWeight { get; set; }

    public List<QaQcReceivingLotDto> Lots { get; set; } = new();
}