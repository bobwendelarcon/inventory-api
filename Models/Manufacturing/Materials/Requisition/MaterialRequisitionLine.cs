namespace inventory_api.Models.Manufacturing.Materials.Requisitions
{
    public class MaterialRequisitionLine
    {
        public int RequisitionLineId { get; set; }

        public int RequisitionId { get; set; }

        public int MaterialId { get; set; }

        public int? MaterialLotId { get; set; }

        public string? LotNo { get; set; }

        public decimal RequestedQuantity { get; set; }

        public decimal? ActualQuantity { get; set; }

        public string Uom { get; set; } = string.Empty;

        public DateTime? ExpirationDate { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public MaterialRequisition? Requisition { get; set; }
    }
}