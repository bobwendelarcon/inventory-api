using inventory_api.Data;
using inventory_api.DTOs.Inventory.RawMaterials;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Inventory
{
    public class RawMaterialInventoryService
    {
        private readonly AppDbContext _context;

        public RawMaterialInventoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RawMaterialInventoryResponseDto> GetInventoryAsync(
     RawMaterialInventoryFilterDto filter)
        {
            filter ??= new RawMaterialInventoryFilterDto();

            var today = DateTime.Today;

            var query =
                from lot in _context.MaterialLotNumbers.AsNoTracking()

                join material in _context.Materials.AsNoTracking()
                    on lot.material_id equals material.material_id

                join category in _context.MaterialCategories.AsNoTracking()
                    on material.material_category_id equals category.material_category_id
                    into categoryJoin
                from category in categoryJoin.DefaultIfEmpty()

                join branch in _context.Branches.AsNoTracking()
                    on lot.branch_id equals branch.branch_id
                    into branchJoin
                from branch in branchJoin.DefaultIfEmpty()

                join supplier in _context.Suppliers.AsNoTracking()
                    on lot.supplier_id equals supplier.SupplierId
                    into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()

                where
                    lot.is_active &&
                    material.is_active &&
                    !material.is_deleted

                select new
                {
                    Lot = lot,
                    Material = material,
                    Category = category,
                    Branch = branch,
                    Supplier = supplier
                };

            // Search
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();

                query = query.Where(x =>
                    x.Material.material_code.ToLower().Contains(search) ||
                    x.Material.material_name.ToLower().Contains(search) ||
                    x.Lot.lot_no.ToLower().Contains(search) ||
                    (x.Category != null &&
                     x.Category.category_name.ToLower().Contains(search)) ||
                    (x.Supplier != null &&
                     x.Supplier.SupplierName.ToLower().Contains(search)));
            }

            // Branch
            if (!string.IsNullOrWhiteSpace(filter.BranchId))
            {
                var branchId = filter.BranchId.Trim();

                query = query.Where(x =>
                    x.Lot.branch_id == branchId);
            }

            // Category
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.Material.material_category_id == filter.CategoryId.Value);
            }

            // Inventory creation date
            if (filter.FromDate.HasValue)
            {
                var fromDate = filter.FromDate.Value.Date;

                query = query.Where(x =>
                    x.Lot.created_at >= fromDate);
            }

            if (filter.ToDate.HasValue)
            {
                var toDateExclusive = filter.ToDate.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.Lot.created_at < toDateExclusive);
            }

            var records = await query
                .OrderBy(x => x.Material.material_name)
                .ThenBy(x => x.Lot.expiration_date)
                .ThenBy(x => x.Lot.lot_no)
                .ToListAsync();

            var result = records.Select(x =>
            {
                var quantity = x.Lot.quantity;
                var minimumStock = x.Material.minimum_stock;

                string stockStatus;

                if (quantity <= 0)
                {
                    stockStatus = "OUT_OF_STOCK";
                }
                else if (minimumStock > 0 && quantity <= minimumStock)
                {
                    stockStatus = "LOW_STOCK";
                }
                else
                {
                    stockStatus = "IN_STOCK";
                }

                int? daysUntilExpiration = null;
                string expiryStatus;

                if (!x.Lot.expiration_date.HasValue)
                {
                    expiryStatus = "NO_EXPIRATION";
                }
                else
                {
                    daysUntilExpiration =
                        (x.Lot.expiration_date.Value.Date - today).Days;

                    if (daysUntilExpiration < 0)
                    {
                        expiryStatus = "EXPIRED";
                    }
                    else if (daysUntilExpiration <= 30)
                    {
                        expiryStatus = "NEAR_EXPIRY";
                    }
                    else
                    {
                        expiryStatus = "GOOD";
                    }
                }

                var inventoryAgeDays =
                    Math.Max(0, (today - x.Lot.created_at.Date).Days);

                var isInternalNonLot =
                    x.Lot.lot_no.StartsWith(
                        "NON-LOT",
                        StringComparison.OrdinalIgnoreCase) ||
                    x.Lot.lot_no.StartsWith(
                        "NOLOT",
                        StringComparison.OrdinalIgnoreCase);

                var lotDisplay =
                    !x.Material.is_lot_tracked || isInternalNonLot
                        ? "Not Lot Tracked"
                        : x.Lot.lot_no;

                return new RawMaterialInventoryListDto
                {
                    MaterialLotId = x.Lot.material_lot_id,

                    MaterialId = x.Material.material_id,
                    MaterialCode = x.Material.material_code,
                    MaterialName = x.Material.material_name,

                    CategoryId = x.Material.material_category_id,
                    CategoryName = x.Category?.category_name ?? "Uncategorized",

                    BranchId = x.Lot.branch_id,

                    // Change branch_name if your Branch model uses another property.
                    BranchName = x.Branch?.branch_name ?? x.Lot.branch_id,

                    IsLotTracked = x.Material.is_lot_tracked,

                    LotNo = x.Lot.lot_no,
                    LotDisplay = lotDisplay,

                    ManufacturingDate = x.Lot.manufacturing_date,
                    ExpirationDate = x.Lot.expiration_date,

                    Quantity = x.Lot.quantity,

                    Uom = string.IsNullOrWhiteSpace(x.Lot.uom)
                        ? x.Material.uom
                        : x.Lot.uom,

                    SupplierId = x.Lot.supplier_id,
                    SupplierName = x.Supplier?.SupplierName ?? "Not Specified",

                    MinimumStock = x.Material.minimum_stock,

                    StockStatus = stockStatus,
                    ExpiryStatus = expiryStatus,

                    InventoryAgeDays = inventoryAgeDays,
                    DaysUntilExpiration = daysUntilExpiration,

                    Remarks = x.Lot.remarks,

                    CreatedAt = x.Lot.created_at,
                    UpdatedAt = x.Lot.updated_at
                };
            }).ToList();

            // Computed-status filters
            if (!string.IsNullOrWhiteSpace(filter.StockStatus))
            {
                var stockStatus = filter.StockStatus.Trim().ToUpper();

                result = result
                    .Where(x => x.StockStatus == stockStatus)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter.ExpiryStatus))
            {
                var expiryStatus = filter.ExpiryStatus.Trim().ToUpper();

                result = result
                    .Where(x => x.ExpiryStatus == expiryStatus)
                    .ToList();
            }

            var response = new RawMaterialInventoryResponseDto
            {
                Summary = new RawMaterialInventorySummaryDto
                {
                    TotalMaterials = result
             .Select(x => x.MaterialId)
             .Distinct()
             .Count(),

                    InventoryLots = result.Count,

                    LowStock = result.Count(x =>
                        x.StockStatus == "LOW_STOCK"),

                    NearExpiry = result.Count(x =>
                        x.ExpiryStatus == "NEAR_EXPIRY"),

                    Expired = result.Count(x =>
                        x.ExpiryStatus == "EXPIRED")
                },

                Items = result
            };

            return response;
        }


        public async Task<List<RawMaterialInventoryTransactionDto>>
    GetTransactionsAsync(int materialLotId)
        {
            var lot = await _context.MaterialLotNumbers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.material_lot_id == materialLotId &&
                    x.is_active);

            if (lot == null)
            {
                throw new KeyNotFoundException(
                    $"Material inventory lot ID {materialLotId} was not found.");
            }

            var transactions = await (
     from transaction in _context.MaterialInventoryTransactions.AsNoTracking()

     join user in _context.Users.AsNoTracking()
         on transaction.encoded_by equals user.user_id
         into userJoin

     from user in userJoin.DefaultIfEmpty()

     where
         transaction.material_id == lot.material_id &&
         transaction.branch_id == lot.branch_id &&
         transaction.lot_no == lot.lot_no

     orderby
         transaction.transaction_date,
         transaction.transaction_id

     select new
     {
         Transaction = transaction,

         EncodedByName = user != null
             ? user.full_name
             : transaction.encoded_by
     }
 ).ToListAsync();

            decimal runningBalance = 0;

            var result = new List<RawMaterialInventoryTransactionDto>();

            foreach (var record in transactions)
            {
                var transaction = record.Transaction;

                var isOutbound = IsOutboundTransaction(
                    transaction.transaction_type);

                var absoluteQuantity =
                    Math.Abs(transaction.quantity);

                var quantityIn = isOutbound
                    ? 0
                    : absoluteQuantity;

                var quantityOut = isOutbound
                    ? absoluteQuantity
                    : 0;

                runningBalance += quantityIn;
                runningBalance -= quantityOut;

                result.Add(new RawMaterialInventoryTransactionDto
                {
                    TransactionId = transaction.transaction_id,

                    MaterialId = transaction.material_id,
                    BranchId = transaction.branch_id,
                    LotNo = transaction.lot_no ?? string.Empty,

                    TransactionType =
                        transaction.transaction_type,

                    Quantity = transaction.quantity,
                    QuantityIn = quantityIn,
                    QuantityOut = quantityOut,
                    RunningBalance = runningBalance,

                    Uom = transaction.uom,

                    ReferenceType =
                        transaction.reference_type ?? string.Empty,

                    ReferenceId =
                        transaction.reference_id,

                    ReferenceNo =
                        transaction.reference_no ?? string.Empty,

                    Remarks =
                        transaction.remarks ?? string.Empty,

                    EncodedBy =
                        record.EncodedByName ?? string.Empty,

                    TransactionDate =
                        transaction.transaction_date
                });
            }

            // Show newest transaction first, but preserve the calculated balance.
            return result
                .OrderByDescending(x => x.TransactionDate)
                .ThenByDescending(x => x.TransactionId)
                .ToList();
        }

        private static bool IsOutboundTransaction(string? transactionType)
        {
            var type = transactionType?
                .Trim()
                .ToUpperInvariant() ?? string.Empty;

            return type.Contains("ISSUE") ||
                   type.Contains("OUT") ||
                   type.Contains("RELEASE") ||
                   type.Contains("CONSUME") ||
                   type.Contains("USAGE") ||
                   type.Contains("TRANSFER_OUT") ||
                   type.Contains("ADJUSTMENT_OUT");
        }



    }
}