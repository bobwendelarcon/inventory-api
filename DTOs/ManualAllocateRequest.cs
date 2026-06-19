namespace inventory_api.DTOs
{
    public class ManualAllocateRequest
    {
        public List<ManualAllocateLineRequest> Lines { get; set; } = new();
    }

    public class ManualAllocateLineRequest
    {
        public long OrderLineId { get; set; }
        public List<ManualAllocateLotRequest> Lots { get; set; } = new();
    }

    public class ManualAllocateLotRequest
    {
        public string? BranchId { get; set; } // ADD
        public string LotNo { get; set; } = string.Empty;
        public decimal AllocateQty { get; set; }
    }
}