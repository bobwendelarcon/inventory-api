namespace inventory_api.DTOs.Reports.RawMaterials
{
    public class RawMaterialReleaseReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? BranchId { get; set; }
        public int? MaterialId { get; set; }

        public string? Search { get; set; }
    }

    public class RawMaterialReleaseReportResponseDto
    {
        public RawMaterialReleaseSummaryDto Summary { get; set; }
            = new();

        public List<RawMaterialReleaseByUomDto> ReleasedByUom { get; set; }
            = new();

        public List<RawMaterialReleaseReportItemDto> Items { get; set; }
            = new();
    }

    public class RawMaterialReleaseSummaryDto
    {
        public int TotalMrsReleased { get; set; }

        public int TotalReleaseTransactions { get; set; }

        public int UniqueMaterials { get; set; }

        public int TotalBranches { get; set; }
    }

    public class RawMaterialReleaseByUomDto
    {
        public string Uom { get; set; } = "";

        public decimal Quantity { get; set; }
    }

    public class RawMaterialReleaseReportItemDto
    {
        public int TransactionId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = "";

        public string MaterialName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public string BranchId { get; set; } = "";

        public string BranchName { get; set; } = "";

        public string LotNo { get; set; } = "";

        public string LotDisplay { get; set; } = "";

        public decimal QuantityReleased { get; set; }

        public string Uom { get; set; } = "";

        public int? RequisitionId { get; set; }

        public string RequisitionNo { get; set; } = "";

        public string ReleasedBy { get; set; } = "";

        public string ReleasedByName { get; set; } = "";

        public string ReceivedBy { get; set; } = "";

        public string ReceivedByName { get; set; } = "";

        public string VerifiedBy { get; set; } = "";

        public string VerifiedByName { get; set; } = "";

        public string PostedBy { get; set; } = "";

        public string PostedByName { get; set; } = "";

        public DateTime TransactionDate { get; set; }

        public DateTime? TimeServed { get; set; }

        public string Remarks { get; set; } = "";
    }
}