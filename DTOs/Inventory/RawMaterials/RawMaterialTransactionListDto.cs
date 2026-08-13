namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialTransactionFilterDto
    {
        public string? Search { get; set; }
        public string? BranchId { get; set; }
        public string? Movement { get; set; }
        public string? TransactionType { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class RawMaterialTransactionListDto
    {
        public int TransactionId { get; set; }

        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public string LotNo { get; set; } = string.Empty;
        public string LotDisplay { get; set; } = string.Empty;

        public string TransactionType { get; set; } = string.Empty;
        public string Movement { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }

        public decimal RunningBalance { get; set; }

        public string Uom { get; set; } = string.Empty;

        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;

        public string EncodedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; }
    }

    public class RawMaterialTransactionSummaryDto
    {
        public int TotalTransactions { get; set; }
        public int InTransactions { get; set; }
        public int OutTransactions { get; set; }
        public int TodayTransactions { get; set; }
    }

    public class RawMaterialTransactionResponseDto
    {
        public RawMaterialTransactionSummaryDto Summary { get; set; }
            = new();

        public List<RawMaterialTransactionListDto> Items { get; set; }
            = new();
    }
}