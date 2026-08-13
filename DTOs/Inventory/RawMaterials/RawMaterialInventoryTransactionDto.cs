namespace inventory_api.DTOs.Inventory.RawMaterials
{
    public class RawMaterialInventoryTransactionDto
    {
        public int TransactionId { get; set; }

        public int MaterialId { get; set; }
        public string BranchId { get; set; } = string.Empty;
        public string LotNo { get; set; } = string.Empty;

        public string TransactionType { get; set; } = string.Empty;

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

        public string Remarks { get; set; } = string.Empty;
        public string EncodedBy { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; }
    }
}