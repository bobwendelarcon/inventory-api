namespace inventory_api.DTOs
{
    public class DailyOrderDetailsDto
    {
        public long OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? SourceBranchId { get; set; }
        public string? SourceBranchName { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public string? ClassName { get; set; }
        public string? RouteName { get; set; }
        public string? SpecialInstructions { get; set; }
        public List<DailyOrderLineDto> Lines { get; set; } = new();
    }

    public class DailyOrderLineDto
    {
        public long OrderLineId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal AvailableBeforeAllocation { get; set; }

        public string? Uom { get; set; }
        public decimal? PackQty { get; set; }
        public string? PackUom { get; set; }

        public string AllocationResult { get; set; } = string.Empty;
        public List<DailyOrderAllocationDto> Allocations { get; set; } = new();
    }

    public class DailyOrderAllocationDto
    {
        public string? BranchId { get; set; }
        public string LotNo { get; set; } = string.Empty;
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public decimal OnHandQty { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal AllocatedQty { get; set; }

        public string? Uom { get; set; }
        public decimal? PackQty { get; set; }
        public string? PackUom { get; set; }
        public int PriorityRank { get; set; }
    }
}
