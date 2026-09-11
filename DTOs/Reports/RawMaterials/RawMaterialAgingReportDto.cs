namespace inventory_api.DTOs.Reports.RawMaterials
{
    public class RawMaterialAgingReportFilterDto
    {
        public string? BranchId { get; set; }
        public int? CategoryId { get; set; }
        public string? Search { get; set; }

        public string? MovementStatus { get; set; }

        public int? MinimumDaysIdle { get; set; }
        public int? MaximumDaysIdle { get; set; }
    }


    public class RawMaterialAgingReportResponseDto
    {
        public RawMaterialAgingReportSummaryDto Summary { get; set; }
            = new();

        public List<RawMaterialAgingReportItemDto> Items { get; set; }
            = new();
    }


    public class RawMaterialAgingReportSummaryDto
    {
        public int TotalLots { get; set; }

        public int Active { get; set; }

        public int SlowMoving { get; set; }

        public int NonMoving { get; set; }

        public int NearExpiry { get; set; }

        public int Expired { get; set; }
    }


    public class RawMaterialAgingReportItemDto
    {
        public int MaterialLotId { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";

        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "";

        public string BranchId { get; set; } = "";
        public string BranchName { get; set; } = "";

        public bool IsLotTracked { get; set; }

        public string LotNo { get; set; } = "";
        public string LotDisplay { get; set; } = "";

        public decimal Quantity { get; set; }

        public string Uom { get; set; } = "";

        public DateTime? DateIn { get; set; }

        public DateTime? LastReleaseDate { get; set; }

        public DateTime? LastMovementDate { get; set; }

        public string LastMovementType { get; set; } = "";

        public string LastMovement { get; set; } = "";

        public int DaysInInventory { get; set; }

        public int? DaysSinceLastMovement { get; set; }

        public int? DaysSinceLastRelease { get; set; }

        public DateTime? ManufacturingDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public int? DaysToExpiry { get; set; }

        public string MovementStatus { get; set; } = "";

        public string ExpiryStatus { get; set; } = "";

        public string SupplierName { get; set; } = "";
    }
}