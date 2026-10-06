using inventory_api.Data;
using inventory_api.DTOs.Inventory.RawMaterials;
using inventory_api.Models.Manufacturing.Materials;
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

            var todayUtc = DateTime.UtcNow;

            var today =
                TimeZoneInfo.ConvertTimeFromUtc(
                    todayUtc,
                    PhilippineTimeZone
                ).Date;

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

            if (filter.FromDate.HasValue)
            {
                var fromUtc =
                    PhilippineDateToUtc(
                        filter.FromDate.Value
                    );

                query = query.Where(x =>
                    x.Lot.created_at >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtcExclusive =
                    PhilippineDateToUtc(
                        filter.ToDate.Value.Date.AddDays(1)
                    );

                query = query.Where(x =>
                    x.Lot.created_at < toUtcExclusive);
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
                var stockStatus =
                    filter.StockStatus
                        .Trim()
                        .ToUpperInvariant();

                if (stockStatus == "AVAILABLE")
                {
                    result = result
                        .Where(x =>
                            x.StockStatus != "OUT_OF_STOCK")
                        .ToList();
                }
                else
                {
                    result = result
                        .Where(x =>
                            x.StockStatus == stockStatus)
                        .ToList();
                }
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

                    InventoryLots = result.Count(x =>
                        x.IsLotTracked),

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
    from transaction in
        _context.MaterialInventoryTransactions.AsNoTracking()

    join user in
        _context.Users.AsNoTracking()
        on transaction.encoded_by equals user.user_id
        into userJoin

    from user in userJoin.DefaultIfEmpty()

    join supplier in
        _context.Suppliers.AsNoTracking()
        on transaction.supplier_id equals supplier.SupplierId
        into supplierJoin

    from supplier in supplierJoin.DefaultIfEmpty()

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

        SupplierName =
            supplier != null
                ? supplier.SupplierName
                : "Not Specified",

        EncodedByName =
            user != null
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

                    SupplierId =
    transaction.supplier_id,

                    SupplierName =
    record.SupplierName,

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


        public async Task<RawMaterialTransactionResponseDto>
    GetAllTransactionsAsync(
        RawMaterialTransactionFilterDto filter)
        {
            filter ??= new RawMaterialTransactionFilterDto();

            var records = await (
                from transaction in
                    _context.MaterialInventoryTransactions.AsNoTracking()

                join material in
                    _context.Materials.AsNoTracking()
                    on transaction.material_id equals material.material_id

                join branch in
                    _context.Branches.AsNoTracking()
                    on transaction.branch_id equals branch.branch_id
                    into branchJoin

                from branch in branchJoin.DefaultIfEmpty()

                join supplier in
    _context.Suppliers.AsNoTracking()
    on transaction.supplier_id equals supplier.SupplierId
    into supplierJoin

                from supplier in supplierJoin.DefaultIfEmpty()

                join user in
                    _context.Users.AsNoTracking()
                    on transaction.encoded_by equals user.user_id
                    into userJoin

                from user in userJoin.DefaultIfEmpty()

                where material.is_active &&
                      !material.is_deleted

                orderby
                    transaction.transaction_date,
                    transaction.transaction_id

                select new
                {
                    Transaction = transaction,
                    Material = material,

                    BranchName =
         branch != null
             ? branch.branch_name
             : transaction.branch_id,

                    SupplierName =
         supplier != null
             ? supplier.SupplierName
             : "Not Specified",

                    EncodedByName =
         user != null
             ? user.full_name
             : transaction.encoded_by
                }
            ).ToListAsync();


            // ----------------------------------------------------
            // Calculate running balance PER:
            // Material + Branch + Lot
            // ----------------------------------------------------

            var balances =
                new Dictionary<string, decimal>();

            var allItems =
                new List<RawMaterialTransactionListDto>();

            foreach (var record in records)
            {
                var transaction = record.Transaction;

                var lotNo =
                    transaction.lot_no?.Trim()
                    ?? string.Empty;

                var balanceKey =
                    $"{transaction.material_id}|" +
                    $"{transaction.branch_id}|" +
                    $"{lotNo}";

                if (!balances.ContainsKey(balanceKey))
                {
                    balances[balanceKey] = 0m;
                }

                var isOutbound =
                    IsOutboundTransaction(
                        transaction.transaction_type);

                var absoluteQty =
                    Math.Abs(transaction.quantity);

                var qtyIn =
                    isOutbound
                        ? 0m
                        : absoluteQty;

                var qtyOut =
                    isOutbound
                        ? absoluteQty
                        : 0m;

                balances[balanceKey] += qtyIn;
                balances[balanceKey] -= qtyOut;

                var isInternalNonLot =
                    lotNo.StartsWith(
                        "NON-LOT",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    lotNo.StartsWith(
                        "NOLOT",
                        StringComparison.OrdinalIgnoreCase);

                allItems.Add(
                    new RawMaterialTransactionListDto
                    {
                        TransactionId =
                            transaction.transaction_id,

                        MaterialId =
                            transaction.material_id,

                        MaterialCode =
                            record.Material.material_code,

                        MaterialName =
                            record.Material.material_name,

                        BranchId =
                            transaction.branch_id,

                        BranchName =
                            record.BranchName,

                        LotNo =
                            lotNo,

                        LotDisplay =
                            !record.Material.is_lot_tracked ||
                            isInternalNonLot
                                ? "Not Lot Tracked"
                                : lotNo,

                        TransactionType =
                            transaction.transaction_type,

                        Movement =
                            isOutbound
                                ? "OUT"
                                : "IN",

                        Quantity =
                            transaction.quantity,

                        QuantityIn =
                            qtyIn,

                        QuantityOut =
                            qtyOut,

                        RunningBalance =
                            balances[balanceKey],

                        Uom =
                            transaction.uom,

                        SupplierId =
    transaction.supplier_id,

                        SupplierName =
    record.SupplierName,

                        ReferenceType =
                            transaction.reference_type
                            ?? string.Empty,

                        ReferenceId =
                            transaction.reference_id,

                        ReferenceNo =
                            transaction.reference_no
                            ?? string.Empty,

                        EncodedBy =
                            record.EncodedByName
                            ?? string.Empty,

                        Remarks =
                            transaction.remarks
                            ?? string.Empty,

                        TransactionDate =
                            transaction.transaction_date
                    });
            }


            // ----------------------------------------------------
            // FILTERS
            //
            // Apply AFTER running balance is calculated so date
            // filtering does not destroy the real balance.
            // ----------------------------------------------------

            IEnumerable<RawMaterialTransactionListDto> filtered =
                allItems;


            // Search
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search =
                    filter.Search
                        .Trim()
                        .ToLowerInvariant();

                filtered = filtered.Where(x =>
                    x.MaterialCode
                        .ToLowerInvariant()
                        .Contains(search)
                    ||
                    x.MaterialName
                        .ToLowerInvariant()
                        .Contains(search)
                    ||
                    x.ReferenceNo
                        .ToLowerInvariant()
                        .Contains(search)
                    ||
                    x.EncodedBy
    .ToLowerInvariant()
    .Contains(search)
||
x.SupplierName
    .ToLowerInvariant()
    .Contains(search));
            }


            // Branch
            if (!string.IsNullOrWhiteSpace(filter.BranchId))
            {
                var branchId =
                    filter.BranchId.Trim();

                filtered = filtered.Where(x =>
                    x.BranchId == branchId);
            }


            // IN / OUT
            if (!string.IsNullOrWhiteSpace(filter.Movement))
            {
                var movement =
                    filter.Movement
                        .Trim()
                        .ToUpperInvariant();

                filtered = filtered.Where(x =>
                    x.Movement == movement);
            }


            // Transaction Type
            if (!string.IsNullOrWhiteSpace(
                filter.TransactionType))
            {
                var transactionType =
                    filter.TransactionType
                        .Trim()
                        .ToUpperInvariant();

                filtered = filtered.Where(x =>
                    x.TransactionType
                        .ToUpperInvariant()
                        == transactionType);
            }


            // Date From - selected date is Philippine date,
            // database transaction_date is UTC.
            if (filter.FromDate.HasValue)
            {
                var fromUtc =
                    PhilippineDateToUtc(
                        filter.FromDate.Value
                    );

                filtered = filtered.Where(x =>
                    x.TransactionDate >= fromUtc);
            }


            // Date To - exclusive next Philippine midnight,
            // converted to UTC.
            if (filter.ToDate.HasValue)
            {
                var toUtcExclusive =
                    PhilippineDateToUtc(
                        filter.ToDate.Value.Date.AddDays(1)
                    );

                filtered = filtered.Where(x =>
                    x.TransactionDate < toUtcExclusive);
            }

            var result = filtered
                .OrderByDescending(x =>
                    x.TransactionDate)
                .ThenByDescending(x =>
                    x.TransactionId)
                .ToList();


            var phToday =
       TimeZoneInfo.ConvertTimeFromUtc(
           DateTime.UtcNow,
           PhilippineTimeZone
       ).Date;

            var todayStartUtc =
                PhilippineDateToUtc(phToday);

            var tomorrowStartUtc =
                PhilippineDateToUtc(
                    phToday.AddDays(1)
                );

            return new RawMaterialTransactionResponseDto
            {
                Summary =
                    new RawMaterialTransactionSummaryDto
                    {
                        TotalTransactions =
                            result.Count,

                        InTransactions =
                            result.Count(x =>
                                x.Movement == "IN"),

                        OutTransactions =
                            result.Count(x =>
                                x.Movement == "OUT"),

                        TodayTransactions =
    result.Count(x =>
        x.TransactionDate >= todayStartUtc &&
        x.TransactionDate < tomorrowStartUtc)
                    },

                Items = result
            };
        }

        public async Task ManualStockInAsync(ManualStockInDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (dto.MaterialId <= 0)
            {
                throw new InvalidOperationException(
                    "Material is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.BranchId))
            {
                throw new InvalidOperationException(
                    "Branch is required.");
            }

            if (dto.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Stock in quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(dto.EncodedBy))
            {
                throw new InvalidOperationException(
                    "Encoded by is required.");
            }

            var material = await _context.Materials
                .FirstOrDefaultAsync(x =>
                    x.material_id == dto.MaterialId &&
                    x.is_active &&
                    !x.is_deleted);

            if (material == null)
            {
                throw new KeyNotFoundException(
                    $"Material ID {dto.MaterialId} was not found.");
            }

            var branchId = dto.BranchId.Trim();
            var now = DateTime.UtcNow;

            string lotNo;

            // ---------------------------------------------------------
            // LOT NUMBER
            // ---------------------------------------------------------

            if (material.is_lot_tracked)
            {
                if (string.IsNullOrWhiteSpace(dto.LotNo))
                {
                    throw new InvalidOperationException(
                        "Lot number is required for this material.");
                }

                lotNo = dto.LotNo.Trim();

                if (dto.ExpirationDate.HasValue &&
                    dto.ManufacturingDate.HasValue &&
                    dto.ExpirationDate.Value.Date <
                    dto.ManufacturingDate.Value.Date)
                {
                    throw new InvalidOperationException(
                        "Expiration date cannot be earlier than manufacturing date.");
                }
            }
            else
            {
                // Keep the same stable internal key used by QC inventory posting.
                // Branch is already stored separately in branch_id.
                lotNo = $"NON-LOT-MAT-{dto.MaterialId}";
            }

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // ---------------------------------------------------------
                // FIND EXISTING INVENTORY LOT
                // ---------------------------------------------------------

                var inventoryLot =
                    await _context.MaterialLotNumbers
                        .FirstOrDefaultAsync(x =>
                            x.material_id == dto.MaterialId &&
                            x.branch_id == branchId &&
                            x.lot_no == lotNo);

                // ---------------------------------------------------------
                // CREATE NEW INVENTORY LOT
                // ---------------------------------------------------------

                if (inventoryLot == null)
                {
                    inventoryLot = new MaterialLotNumber
                    {
                        material_id = dto.MaterialId,

                        branch_id = branchId,

                        lot_no = lotNo,

                        manufacturing_date =
                            material.is_lot_tracked
                                ? dto.ManufacturingDate
                                : null,

                        expiration_date =
                            material.is_lot_tracked
                                ? dto.ExpirationDate
                                : null,

                        quantity = dto.Quantity,

                        uom = material.uom,

                        supplier_id =
    material.is_lot_tracked
        ? dto.SupplierId
        : null,

                        remarks =
                            string.IsNullOrWhiteSpace(dto.Remarks)
                                ? "Manual stock in."
                                : dto.Remarks.Trim(),

                        is_active = true,

                        created_at = now,

                        updated_at = null
                    };

                    await _context.MaterialLotNumbers
                        .AddAsync(inventoryLot);
                }

                // ---------------------------------------------------------
                // UPDATE EXISTING INVENTORY LOT
                // ---------------------------------------------------------

                else
                {
                    inventoryLot.quantity += dto.Quantity;

                    inventoryLot.is_active = true;

                    inventoryLot.updated_at = now;

                    /*
                     * Don't replace existing lot information.
                     * Only fill missing values.
                     */

                    if (material.is_lot_tracked)
                    {
                        inventoryLot.manufacturing_date ??=
                            dto.ManufacturingDate;

                        inventoryLot.expiration_date ??=
                            dto.ExpirationDate;
                    }

                    if (material.is_lot_tracked)
                    {
                        inventoryLot.supplier_id ??=
                            dto.SupplierId;
                    }
                    else
                    {
                        inventoryLot.supplier_id = null;
                    }

                    if (string.IsNullOrWhiteSpace(
                            inventoryLot.uom))
                    {
                        inventoryLot.uom =
                            material.uom;
                    }
                }

                // ---------------------------------------------------------
                // CREATE INVENTORY TRANSACTION
                // ---------------------------------------------------------

                var inventoryTransaction =
       new MaterialInventoryTransaction
       {
           material_id =
               dto.MaterialId,

           branch_id =
               branchId,

           lot_no =
               lotNo,

           transaction_type =
               "MANUAL_STOCK_IN",

           quantity =
               dto.Quantity,

           uom =
               material.uom,

           supplier_id =
               dto.SupplierId,

           reference_type =
               "MANUAL",

           reference_id =
               null,

           reference_no =
               $"MSI-{now:yyyyMMddHHmmssfff}",

           remarks =
               string.IsNullOrWhiteSpace(dto.Remarks)
                   ? "Manual stock in."
                   : dto.Remarks.Trim(),

           encoded_by =
               dto.EncodedBy.Trim(),

           transaction_date =
               now,

           created_at =
               now
       };

                await _context.MaterialInventoryTransactions
                    .AddAsync(inventoryTransaction);

                // ---------------------------------------------------------
                // SAVE BOTH INVENTORY + TRANSACTION
                // ---------------------------------------------------------

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }


        public async Task AdjustStockAsync(
    AdjustRawMaterialStockDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (dto.MaterialLotId <= 0)
            {
                throw new InvalidOperationException(
                    "Inventory lot is required.");
            }

            if (dto.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Adjustment quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(dto.AdjustmentType))
            {
                throw new InvalidOperationException(
                    "Adjustment type is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                throw new InvalidOperationException(
                    "Adjustment reason is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.EncodedBy))
            {
                throw new InvalidOperationException(
                    "Encoded by is required.");
            }

            var adjustmentType =
                dto.AdjustmentType
                    .Trim()
                    .ToUpperInvariant();

            if (adjustmentType != "INCREASE" &&
                adjustmentType != "DECREASE")
            {
                throw new InvalidOperationException(
                    "Adjustment type must be INCREASE or DECREASE.");
            }

            var inventoryLot =
                await _context.MaterialLotNumbers
                    .FirstOrDefaultAsync(x =>
                        x.material_lot_id == dto.MaterialLotId &&
                        x.is_active);

            if (inventoryLot == null)
            {
                throw new KeyNotFoundException(
                    $"Inventory lot ID {dto.MaterialLotId} was not found.");
            }

            var material =
                await _context.Materials
                    .FirstOrDefaultAsync(x =>
                        x.material_id == inventoryLot.material_id &&
                        x.is_active &&
                        !x.is_deleted);

            if (material == null)
            {
                throw new KeyNotFoundException(
                    "Material was not found.");
            }

            // Prevent negative inventory
            if (adjustmentType == "DECREASE" &&
                dto.Quantity > inventoryLot.quantity)
            {
                throw new InvalidOperationException(
                    $"Adjustment cannot exceed current stock of " +
                    $"{inventoryLot.quantity:0.####} {inventoryLot.uom}.");
            }

            var now = DateTime.UtcNow;

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                decimal transactionQuantity;
                string transactionType;

                if (adjustmentType == "INCREASE")
                {
                    inventoryLot.quantity +=
                        dto.Quantity;

                    transactionQuantity =
                        dto.Quantity;

                    transactionType =
                        "MANUAL_ADJUSTMENT_IN";
                }
                else
                {
                    inventoryLot.quantity -=
                        dto.Quantity;

                    transactionQuantity =
                        -dto.Quantity;

                    transactionType =
                        "MANUAL_ADJUSTMENT_OUT";
                }

                inventoryLot.updated_at =
                    now;

                var remarks =
                    $"Reason: {dto.Reason.Trim()}";

                if (!string.IsNullOrWhiteSpace(dto.Remarks))
                {
                    remarks +=
                        $" | {dto.Remarks.Trim()}";
                }

                var inventoryTransaction =
                    new MaterialInventoryTransaction
                    {
                        material_id =
                            inventoryLot.material_id,

                        branch_id =
                            inventoryLot.branch_id,

                        lot_no =
                            inventoryLot.lot_no,

                        transaction_type =
                            transactionType,

                        quantity =
                            transactionQuantity,

                        uom =
                            string.IsNullOrWhiteSpace(
                                inventoryLot.uom)
                                ? material.uom
                                : inventoryLot.uom,

                        supplier_id =
                            inventoryLot.supplier_id,

                        reference_type =
                            "MANUAL_ADJUSTMENT",

                        reference_id =
                            null,

                        reference_no =
                            $"ADJ-{now:yyyyMMddHHmmssfff}",

                        remarks =
                            remarks,

                        encoded_by =
                            dto.EncodedBy.Trim(),

                        transaction_date =
                            now,

                        created_at =
                            now
                    };

                await _context
                    .MaterialInventoryTransactions
                    .AddAsync(inventoryTransaction);

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }


        public async Task<RawMaterialConsolidatedResponseDto>
    GetConsolidatedInventoryAsync(
        RawMaterialConsolidatedFilterDto filter)
        {
            filter ??=
                new RawMaterialConsolidatedFilterDto();


            // =========================================================
            // MATERIAL MASTER
            // Start from Materials so ZERO-STOCK materials still appear.
            // =========================================================

            var materialQuery =
                _context.Materials
                    .AsNoTracking()
                    .Include(x => x.Category)
                    .Include(x => x.SubCategory)
                    .Where(x =>
                        x.is_active &&
                        !x.is_deleted);


            // =========================================================
            // SEARCH
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                filter.Search))
            {
                var search =
                    filter.Search
                        .Trim()
                        .ToLowerInvariant();

                materialQuery =
                    materialQuery.Where(x =>

                        x.material_code
                            .ToLower()
                            .Contains(search)

                        ||

                        x.material_name
                            .ToLower()
                            .Contains(search)

                        ||

                        (
                            x.Category != null &&
                            x.Category.category_name
                                .ToLower()
                                .Contains(search)
                        )

                        ||

                        (
                            x.SubCategory != null &&
                            x.SubCategory.subcategory_name
                                .ToLower()
                                .Contains(search)
                        )
                    );
            }


            // =========================================================
            // CATEGORY
            // =========================================================

            if (filter.CategoryId.HasValue)
            {
                materialQuery =
                    materialQuery.Where(x =>
                        x.material_category_id ==
                        filter.CategoryId.Value);
            }


            // =========================================================
            // SUB CATEGORY
            // =========================================================

            if (filter.SubCategoryId.HasValue)
            {
                materialQuery =
                    materialQuery.Where(x =>
                        x.material_subcategory_id ==
                        filter.SubCategoryId.Value);
            }


            var materials =
                await materialQuery
                    .OrderBy(x =>
                        x.material_name)
                    .ToListAsync();


            if (materials.Count == 0)
            {
                return new RawMaterialConsolidatedResponseDto
                {
                    Summary =
                        new RawMaterialConsolidatedSummaryDto(),

                    Items =
                        new List<
                            RawMaterialConsolidatedListDto>()
                };
            }


            var materialIds =
                materials
                    .Select(x =>
                        x.material_id)
                    .ToList();


            // =========================================================
            // INVENTORY LOTS
            // =========================================================

            var lotQuery =
                _context.MaterialLotNumbers
                    .AsNoTracking()
                    .Where(x =>
                        x.is_active &&
                        materialIds.Contains(
                            x.material_id));


            // Branch filter applies to inventory quantity,
            // NOT to the material master.
            //
            // This means if a material has no stock in BR1,
            // it will still appear as zero in BR1.
            if (!string.IsNullOrWhiteSpace(
                filter.BranchId))
            {
                var branchId =
                    filter.BranchId.Trim();

                lotQuery =
                    lotQuery.Where(x =>
                        x.branch_id ==
                        branchId);
            }


            var lots =
                await lotQuery
                    .ToListAsync();


            // =========================================================
            // BUILD CONSOLIDATED RESULT
            // =========================================================

            var result =
                new List<
                    RawMaterialConsolidatedListDto>();


            foreach (var material in materials)
            {
                var materialLots =
                    lots
                        .Where(x =>
                            x.material_id ==
                            material.material_id)
                        .ToList();


                var totalQuantity =
                    materialLots.Sum(x =>
                        x.quantity);


                string stockStatus;


                if (totalQuantity <= 0)
                {
                    stockStatus =
                        "OUT_OF_STOCK";
                }
                else if (
                    material.minimum_stock > 0 &&
                    totalQuantity <=
                    material.minimum_stock)
                {
                    stockStatus =
                        "LOW_STOCK";
                }
                else
                {
                    stockStatus =
                        "IN_STOCK";
                }


                int? availableLots = null;


                if (material.is_lot_tracked)
                {
                    availableLots =
                        materialLots.Count(x =>
                            x.quantity > 0);
                }


                result.Add(
                    new RawMaterialConsolidatedListDto
                    {
                        MaterialId =
                            material.material_id,

                        MaterialCode =
                            material.material_code,

                        MaterialName =
                            material.material_name,


                        CategoryId =
                            material.material_category_id,

                        CategoryName =
                            material.Category
                                ?.category_name
                            ??
                            "Uncategorized",


                        SubCategoryId =
                            material.material_subcategory_id,

                        SubCategoryName =
                            material.SubCategory
                                ?.subcategory_name
                            ??
                            "No Sub Category",


                        Quantity =
                            totalQuantity,

                        Uom =
                            material.uom,

                        MinimumStock =
                            material.minimum_stock,

                        IsLotTracked =
                            material.is_lot_tracked,

                        AvailableLots =
                            availableLots,

                        StockStatus =
                            stockStatus
                    });
            }


            // =========================================================
            // STOCK STATUS FILTER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                filter.StockStatus))
            {
                var stockStatus =
                    filter.StockStatus
                        .Trim()
                        .ToUpperInvariant();

                result =
                    result
                        .Where(x =>
                            x.StockStatus ==
                            stockStatus)
                        .ToList();
            }


            // =========================================================
            // SUMMARY
            // =========================================================

            var response =
                new RawMaterialConsolidatedResponseDto
                {
                    Summary =
                        new RawMaterialConsolidatedSummaryDto
                        {
                            TotalMaterials =
                                result.Count,

                            InStock =
                                result.Count(x =>
                                    x.StockStatus ==
                                    "IN_STOCK"),

                            LowStock =
                                result.Count(x =>
                                    x.StockStatus ==
                                    "LOW_STOCK"),

                            OutOfStock =
                                result.Count(x =>
                                    x.StockStatus ==
                                    "OUT_OF_STOCK"),

                            AvailableLots =
                                result.Sum(x =>
                                    x.AvailableLots ?? 0)
                        },

                    Items =
                        result
                            .OrderBy(x =>
                                x.MaterialName)
                            .ToList()
                };


            return response;
        }


        public async Task<List<RawMaterialLotDto>>
    GetMaterialLotsAsync(
        int materialId,
        string? branchId = null)
        {
            // =========================================================
            // VALIDATE MATERIAL
            // =========================================================

            var material =
                await _context.Materials
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.material_id == materialId &&
                        x.is_active &&
                        !x.is_deleted);

            if (material == null)
            {
                throw new KeyNotFoundException(
                    $"Material ID {materialId} was not found.");
            }


            // =========================================================
            // GET INVENTORY LOTS
            // =========================================================

            var query =
                from lot in
                    _context.MaterialLotNumbers.AsNoTracking()

                join branch in
                    _context.Branches.AsNoTracking()
                    on lot.branch_id equals branch.branch_id
                    into branchJoin

                from branch in branchJoin.DefaultIfEmpty()

                join supplier in
                    _context.Suppliers.AsNoTracking()
                    on lot.supplier_id equals supplier.SupplierId
                    into supplierJoin

                from supplier in supplierJoin.DefaultIfEmpty()

                where
                    lot.material_id == materialId &&
                    lot.is_active

                select new
                {
                    Lot = lot,
                    Branch = branch,
                    Supplier = supplier
                };


            // =========================================================
            // OPTIONAL BRANCH / WAREHOUSE FILTER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(branchId))
            {
                var selectedBranch =
                    branchId.Trim();

                query =
                    query.Where(x =>
                        x.Lot.branch_id == selectedBranch);
            }


            // =========================================================
            // LOAD RECORDS
            // =========================================================

            var records =
                await query
                    .OrderBy(x =>
                        x.Lot.expiration_date)
                    .ThenBy(x =>
                        x.Lot.lot_no)
                    .ToListAsync();


            // =========================================================
            // BUILD RESULT
            // =========================================================

            var result =
                records.Select(x =>
                {
                    var lotNo =
                        x.Lot.lot_no ?? string.Empty;

                    var isInternalNonLot =
                        lotNo.StartsWith(
                            "NON-LOT",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        lotNo.StartsWith(
                            "NOLOT",
                            StringComparison.OrdinalIgnoreCase);


                    return new RawMaterialLotDto
                    {
                        MaterialLotId =
                            x.Lot.material_lot_id,

                        MaterialId =
                            x.Lot.material_id,

                        BranchId =
                            x.Lot.branch_id,

                        BranchName =
                            x.Branch?.branch_name
                            ?? x.Lot.branch_id,

                        LotNo =
                            lotNo,

                        LotDisplay =
                            !material.is_lot_tracked ||
                            isInternalNonLot
                                ? "Not Lot Tracked"
                                : lotNo,

                        ManufacturingDate =
                            x.Lot.manufacturing_date,

                        ExpirationDate =
                            x.Lot.expiration_date,

                        Quantity =
                            x.Lot.quantity,

                        Uom =
                            string.IsNullOrWhiteSpace(
                                x.Lot.uom)
                                ? material.uom
                                : x.Lot.uom,

                        SupplierId =
                            x.Lot.supplier_id,

                        SupplierName =
                            x.Supplier?.SupplierName
                            ?? "Not Specified"
                    };
                })
                .ToList();


            return result;
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


        public async Task<RawMaterialTransactionDetailsDto>
    GetTransactionByIdAsync(
        int transactionId)
        {
            var result =
                await (
                    from transaction in
                        _context.MaterialInventoryTransactions
                            .AsNoTracking()

                    join material in
                        _context.Materials.AsNoTracking()
                        on transaction.material_id
                        equals material.material_id

                    join branch in
                        _context.Branches.AsNoTracking()
                        on transaction.branch_id
                        equals branch.branch_id
                        into branchJoin

                    from branch in
                        branchJoin.DefaultIfEmpty()

                    join supplier in
                        _context.Suppliers.AsNoTracking()
                        on transaction.supplier_id
                        equals supplier.SupplierId
                        into supplierJoin

                    from supplier in
                        supplierJoin.DefaultIfEmpty()

                    join user in
                        _context.Users.AsNoTracking()
                        on transaction.encoded_by
                        equals user.user_id
                        into userJoin

                    from user in
                        userJoin.DefaultIfEmpty()

                    where
                        transaction.transaction_id ==
                        transactionId

                    select new
                        RawMaterialTransactionDetailsDto
                    {
                        TransactionId =
                                transaction.transaction_id,

                        MaterialId =
                                transaction.material_id,

                        MaterialCode =
                                material.material_code,

                        MaterialName =
                                material.material_name,

                        BranchId =
                                transaction.branch_id,

                        BranchName =
                                branch != null
                                    ? branch.branch_name
                                    : transaction.branch_id,

                        LotNo =
                                transaction.lot_no
                                ?? string.Empty,

                        TransactionType =
                                transaction.transaction_type,

                        Movement =
                                IsOutboundTransaction(
                                    transaction.transaction_type
                                )
                                    ? "OUT"
                                    : "IN",

                        Quantity =
                                transaction.quantity,

                        Uom =
                                transaction.uom,

                        SupplierId =
                                transaction.supplier_id,

                        SupplierName =
                                supplier != null
                                    ? supplier.SupplierName
                                    : "Not Specified",

                        ReferenceType =
                                transaction.reference_type
                                ?? string.Empty,

                        ReferenceId =
                                transaction.reference_id,

                        ReferenceNo =
                                transaction.reference_no
                                ?? string.Empty,

                        Remarks =
                                transaction.remarks
                                ?? string.Empty,

                        EncodedBy =
                                transaction.encoded_by
                                ?? string.Empty,

                        EncodedByName =
                                user != null
                                    ? user.full_name
                                    : transaction.encoded_by
                                      ?? string.Empty,

                        TransactionDate =
                                transaction.transaction_date
                    }
                )
                .FirstOrDefaultAsync();

            if (result == null)
            {
                throw new KeyNotFoundException(
                    $"Inventory transaction ID {transactionId} was not found."
                );
            }

            return result;
        }

        public async Task AddExistingStockAsync(
           ExistingStockDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.MaterialId <= 0)
                throw new InvalidOperationException(
                    "Material is required.");

            if (string.IsNullOrWhiteSpace(dto.BranchId))
                throw new InvalidOperationException(
                    "Branch is required.");

            if (dto.Quantity <= 0)
                throw new InvalidOperationException(
                    "Existing quantity must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.EncodedBy))
                throw new InvalidOperationException(
                    "Encoded by is required.");

            var material =
                await _context.Materials
                    .FirstOrDefaultAsync(x =>
                        x.material_id == dto.MaterialId &&
                        x.is_active &&
                        !x.is_deleted);

            if (material == null)
                throw new KeyNotFoundException(
                    $"Material ID {dto.MaterialId} was not found.");

            var branchId = dto.BranchId.Trim();

            var branchExists =
                await _context.Branches
                    .AnyAsync(x =>
                        x.branch_id == branchId);

            if (!branchExists)
                throw new KeyNotFoundException(
                    $"Branch '{branchId}' was not found.");

            var now = DateTime.UtcNow;

            string lotNo;

            // =========================================================
            // DETERMINE LOT
            // =========================================================

            if (material.is_lot_tracked)
            {
                if (string.IsNullOrWhiteSpace(dto.LotNo))
                {
                    throw new InvalidOperationException(
                        "Lot number is required for this material.");
                }

                lotNo = dto.LotNo.Trim();

                if (dto.ManufacturingDate.HasValue &&
                    dto.ExpirationDate.HasValue &&
                    dto.ExpirationDate.Value.Date <
                    dto.ManufacturingDate.Value.Date)
                {
                    throw new InvalidOperationException(
                        "Expiration date cannot be earlier than manufacturing date.");
                }
            }
            else
            {
                lotNo =
                    $"NON-LOT-MAT-{dto.MaterialId}";
            }

            await using var dbTransaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // =====================================================
                // PREVENT DUPLICATE INITIAL INVENTORY
                // =====================================================

                var initialStockExists =
                    await _context.MaterialInventoryTransactions
                        .AnyAsync(x =>
                            x.material_id == dto.MaterialId &&
                            x.branch_id == branchId &&
                            x.lot_no == lotNo &&
                            x.transaction_type == "INITIAL_STOCK");

                if (initialStockExists)
                {
                    throw new InvalidOperationException(
                        "Existing inventory for this material, " +
                        "warehouse and lot has already been encoded. " +
                        "Use Stock Adjustment if the quantity needs correction.");
                }

                // =====================================================
                // FIND INVENTORY LOT
                // =====================================================

                var inventoryLot =
                    await _context.MaterialLotNumbers
                        .FirstOrDefaultAsync(x =>
                            x.material_id == dto.MaterialId &&
                            x.branch_id == branchId &&
                            x.lot_no == lotNo);

                // =====================================================
                // CREATE / UPDATE INVENTORY LOT
                // =====================================================

                if (inventoryLot == null)
                {
                    inventoryLot =
                        new MaterialLotNumber
                        {
                            material_id =
                                dto.MaterialId,

                            branch_id =
                                branchId,

                            lot_no =
                                lotNo,

                            manufacturing_date =
                                material.is_lot_tracked
                                    ? dto.ManufacturingDate
                                    : null,

                            expiration_date =
                                material.is_lot_tracked
                                    ? dto.ExpirationDate
                                    : null,

                            quantity =
                                dto.Quantity,

                            uom =
                                material.uom,

                            supplier_id =
                                material.is_lot_tracked
                                    ? dto.SupplierId
                                    : null,

                            remarks =
                                string.IsNullOrWhiteSpace(dto.Remarks)
                                    ? "Initial existing inventory."
                                    : dto.Remarks.Trim(),

                            is_active =
                                true,

                            created_at =
                                now
                        };

                    await _context.MaterialLotNumbers
                        .AddAsync(inventoryLot);
                }
                else
                {
                    /*
                     * A lot may already exist because of another
                     * transaction. Existing/opening inventory adds
                     * the physical opening quantity to that lot.
                     */

                    inventoryLot.quantity +=
                        dto.Quantity;

                    inventoryLot.is_active =
                        true;

                    inventoryLot.updated_at =
                        now;

                    if (material.is_lot_tracked)
                    {
                        inventoryLot.manufacturing_date ??=
                            dto.ManufacturingDate;

                        inventoryLot.expiration_date ??=
                            dto.ExpirationDate;

                        inventoryLot.supplier_id ??=
                            dto.SupplierId;
                    }
                }

                // =====================================================
                // INVENTORY TRANSACTION
                // =====================================================

                var transaction =
                    new MaterialInventoryTransaction
                    {
                        material_id =
                            dto.MaterialId,

                        branch_id =
                            branchId,

                        lot_no =
                            lotNo,

                        transaction_type =
                            "INITIAL_STOCK",

                        quantity =
                            dto.Quantity,

                        uom =
                            material.uom,

                        supplier_id =
                            material.is_lot_tracked
                                ? dto.SupplierId
                                : null,

                        reference_type =
                            "INITIAL_INVENTORY",

                        reference_id =
                            null,

                        reference_no =
                            $"INIT-{now:yyyyMMddHHmmssfff}",

                        remarks =
                            string.IsNullOrWhiteSpace(dto.Remarks)
                                ? "Initial existing inventory encoding."
                                : dto.Remarks.Trim(),

                        encoded_by =
                            dto.EncodedBy.Trim(),

                        transaction_date =
                            now,

                        created_at =
                            now
                    };

                await _context.MaterialInventoryTransactions
                    .AddAsync(transaction);

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }


        public async Task ManualStockOutAsync(
    ManualStockOutDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (dto.MaterialLotId <= 0)
            {
                throw new InvalidOperationException(
                    "Inventory lot is required.");
            }

            if (dto.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Stock out quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                throw new InvalidOperationException(
                    "Stock out reason is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.EncodedBy))
            {
                throw new InvalidOperationException(
                    "Encoded by is required.");
            }

            // =========================================================
            // FIND INVENTORY LOT
            // =========================================================

            var inventoryLot =
                await _context.MaterialLotNumbers
                    .FirstOrDefaultAsync(x =>
                        x.material_lot_id == dto.MaterialLotId &&
                        x.is_active);

            if (inventoryLot == null)
            {
                throw new KeyNotFoundException(
                    $"Inventory lot ID {dto.MaterialLotId} was not found.");
            }

            // =========================================================
            // FIND MATERIAL
            // =========================================================

            var material =
                await _context.Materials
                    .FirstOrDefaultAsync(x =>
                        x.material_id == inventoryLot.material_id &&
                        x.is_active &&
                        !x.is_deleted);

            if (material == null)
            {
                throw new KeyNotFoundException(
                    "Material was not found.");
            }

            // =========================================================
            // CHECK AVAILABLE STOCK
            // =========================================================

            if (inventoryLot.quantity <= 0)
            {
                throw new InvalidOperationException(
                    "This material has no available stock.");
            }

            if (dto.Quantity > inventoryLot.quantity)
            {
                throw new InvalidOperationException(
                    $"Stock out quantity cannot exceed available stock of " +
                    $"{inventoryLot.quantity:0.####} " +
                    $"{inventoryLot.uom}.");
            }

            var now = DateTime.UtcNow;
            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // DEDUCT INVENTORY
                // =====================================================

                inventoryLot.quantity -= dto.Quantity;

                inventoryLot.updated_at = now;

                // =====================================================
                // REMARKS
                // =====================================================

                var remarks =
                    $"Reason: {dto.Reason.Trim()}";

                if (!string.IsNullOrWhiteSpace(dto.Remarks))
                {
                    remarks +=
                        $" | {dto.Remarks.Trim()}";
                }

                // =====================================================
                // INVENTORY TRANSACTION
                // =====================================================

                var inventoryTransaction =
                    new MaterialInventoryTransaction
                    {
                        material_id =
                            inventoryLot.material_id,

                        branch_id =
                            inventoryLot.branch_id,

                        lot_no =
                            inventoryLot.lot_no,

                        transaction_type =
                            "MANUAL_STOCK_OUT",

                        // Outbound transactions use negative quantity
                        quantity =
                            -dto.Quantity,

                        uom =
                            string.IsNullOrWhiteSpace(
                                inventoryLot.uom)
                                ? material.uom
                                : inventoryLot.uom,

                        supplier_id =
                            inventoryLot.supplier_id,

                        reference_type =
                            "MANUAL",

                        reference_id =
                            null,

                        reference_no =
                            $"MSO-{now:yyyyMMddHHmmssfff}",

                        remarks =
                            remarks,

                        encoded_by =
                            dto.EncodedBy.Trim(),

                        transaction_date =
                            now,

                        created_at =
                            now
                    };

                await _context
                    .MaterialInventoryTransactions
                    .AddAsync(inventoryTransaction);

                // =====================================================
                // SAVE
                // =====================================================

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
        private static readonly TimeZoneInfo PhilippineTimeZone =
    TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows()
            ? "Singapore Standard Time"
            : "Asia/Manila"
    );

        private static DateTime PhilippineDateToUtc(
            DateTime date)
        {
            var phDate = DateTime.SpecifyKind(
                date.Date,
                DateTimeKind.Unspecified
            );

            return TimeZoneInfo.ConvertTimeToUtc(
                phDate,
                PhilippineTimeZone
            );
        }

    




    }
}