namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialConsolidatedFilterDto
    {
        public string? Search { get; set; }

        public string? BranchId { get; set; }

        public int? CategoryId { get; set; }

        public int? SubCategoryId { get; set; }

        public string? StockStatus { get; set; }
    }


    public class RawMaterialConsolidatedListDto
    {
        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } =
            string.Empty;

        public string MaterialName { get; set; } =
            string.Empty;


        public int? CategoryId { get; set; }

        public string CategoryName { get; set; } =
            string.Empty;


        public int? SubCategoryId { get; set; }

        public string SubCategoryName { get; set; } =
            string.Empty;


        public decimal Quantity { get; set; }

        public string Uom { get; set; } =
            string.Empty;


        public decimal MinimumStock { get; set; }

        public bool IsLotTracked { get; set; }

        public int? AvailableLots { get; set; }

        public string StockStatus { get; set; } =
            string.Empty;
    }


    public class RawMaterialConsolidatedSummaryDto
    {
        public int TotalMaterials { get; set; }

        public int InStock { get; set; }

        public int LowStock { get; set; }

        public int OutOfStock { get; set; }

        public int AvailableLots { get; set; }
    }


    public class RawMaterialConsolidatedResponseDto
    {
        public RawMaterialConsolidatedSummaryDto Summary
        {
            get;
            set;
        } = new();

        public List<RawMaterialConsolidatedListDto> Items
        {
            get;
            set;
        } = new();
    }
}