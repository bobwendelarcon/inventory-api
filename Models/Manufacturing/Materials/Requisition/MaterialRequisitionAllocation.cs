namespace inventory_api.Models.Manufacturing.Materials.Requisitions
{
    public class MaterialRequisitionAllocation
    {
        public int AllocationId { get; set; }

        public int RequisitionLineId { get; set; }

        // Null for non-lot-tracked materials
        public int? MaterialLotId { get; set; }

        public string? LotNo { get; set; }

        // PRODUCTION_PROPOSAL or WAREHOUSE
        public string AllocationStage { get; set; }
            = "PRODUCTION_PROPOSAL";

        // AUTO_FEFO, MANUAL, QUANTITY_ONLY
        public string AllocationMethod { get; set; }
            = "MANUAL";

        public decimal AllocatedQuantity { get; set; }

        public decimal ReleasedQuantity { get; set; }

        public string? AllocatedBy { get; set; }

        public DateTime AllocatedAt { get; set; }

        public string? Remarks { get; set; }

        public MaterialRequisitionLine? RequisitionLine
        { get; set; }
    }
}