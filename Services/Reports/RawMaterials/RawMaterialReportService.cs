using inventory_api.Data;
using inventory_api.DTOs.Reports.RawMaterials;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Reports.RawMaterials
{
    public class RawMaterialReportService
    {
        private readonly AppDbContext _context;

        public RawMaterialReportService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<RawMaterialReleaseReportResponseDto>
            GetReleaseReportAsync(
                RawMaterialReleaseReportFilterDto filter)
        {
            filter ??=
                new RawMaterialReleaseReportFilterDto();

            // =====================================================
            // DEFAULT DATE = TODAY
            // =====================================================

            var fromDate =
                filter.FromDate?.Date
                ?? DateTime.Today;

            var toDate =
                filter.ToDate?.Date
                ?? fromDate;

            var toExclusive =
                toDate.AddDays(1);

            // =====================================================
            // LOAD MATERIAL RELEASE TRANSACTIONS
            // =====================================================

            var query =
                from transaction in
                    _context.MaterialInventoryTransactions
                        .AsNoTracking()

                join material in
                    _context.Materials
                        .AsNoTracking()
                    on transaction.material_id
                    equals material.material_id

                join category in
                    _context.MaterialCategories
                        .AsNoTracking()
                    on material.material_category_id
                    equals category.material_category_id
                    into categoryJoin

                from category in
                    categoryJoin.DefaultIfEmpty()

                join branch in
                    _context.Branches
                        .AsNoTracking()
                    on transaction.branch_id
                    equals branch.branch_id
                    into branchJoin

                from branch in
                    branchJoin.DefaultIfEmpty()

                where
                    transaction.transaction_type
                        == "MATERIAL_RELEASE"

                    && transaction.transaction_date
                        >= fromDate

                    && transaction.transaction_date
                        < toExclusive

                select new
                {
                    Transaction = transaction,
                    Material = material,

                    CategoryName =
                        category != null
                            ? category.category_name
                            : "Uncategorized",

                    BranchName =
                        branch != null
                            ? branch.branch_name
                            : transaction.branch_id
                };

            // =====================================================
            // FILTERS
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    filter.BranchId))
            {
                var branchId =
                    filter.BranchId.Trim();

                query =
                    query.Where(x =>
                        x.Transaction.branch_id
                        == branchId);
            }

            if (filter.MaterialId.HasValue)
            {
                query =
                    query.Where(x =>
                        x.Transaction.material_id
                        == filter.MaterialId.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    filter.Search))
            {
                var search =
                    filter.Search
                        .Trim()
                        .ToLowerInvariant();

                query =
                    query.Where(x =>

                        x.Material.material_code
                            .ToLower()
                            .Contains(search)

                        ||

                        x.Material.material_name
                            .ToLower()
                            .Contains(search)

                        ||

                        (
                            x.Transaction.reference_no != null
                            &&
                            x.Transaction.reference_no
                                .ToLower()
                                .Contains(search)
                        )

                        ||

                        (
                            x.Transaction.lot_no != null
                            &&
                            x.Transaction.lot_no
                                .ToLower()
                                .Contains(search)
                        )
                    );
            }

            var transactions =
                await query
                    .OrderByDescending(x =>
                        x.Transaction.transaction_date)
                    .ThenByDescending(x =>
                        x.Transaction.transaction_id)
                    .ToListAsync();

            // =====================================================
            // GET RELATED MRS HEADERS
            // =====================================================

            var requisitionIds =
                transactions
                    .Where(x =>
                        x.Transaction.reference_type
                        == "MATERIAL_REQUISITION"
                        &&
                        x.Transaction.reference_id.HasValue)
                    .Select(x =>
                        x.Transaction.reference_id!.Value)
                    .Distinct()
                    .ToList();

            var requisitions =
                await _context.MaterialRequisitions
                    .AsNoTracking()
                    .Where(x =>
                        requisitionIds.Contains(
                            x.RequisitionId))
                    .ToDictionaryAsync(
                        x => x.RequisitionId);

            // =====================================================
            // LOAD USERS
            // =====================================================

            var users =
                await _context.Users
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.user_id,
                        x => x.full_name);

            string GetUserName(
                string? userId)
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return "";

                return users.TryGetValue(
                    userId,
                    out var name)
                        ? name
                        : userId;
            }

            // =====================================================
            // BUILD RESULT
            // =====================================================

            var items =
                new List<RawMaterialReleaseReportItemDto>();

            foreach (var record in transactions)
            {
                var transaction =
                    record.Transaction;

                dynamic? requisition = null;

                if (
                    transaction.reference_id.HasValue
                    &&
                    requisitions.TryGetValue(
                        transaction.reference_id.Value,
                        out var foundRequisition)
                )
                {
                    requisition =
                        foundRequisition;
                }

                var lotNo =
                    transaction.lot_no?.Trim()
                    ?? "";

                var isInternalNonLot =
                    lotNo.StartsWith(
                        "NON-LOT",
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    lotNo.StartsWith(
                        "NOLOT",
                        StringComparison.OrdinalIgnoreCase);

                var quantityReleased =
                    Math.Abs(
                        transaction.quantity);

                items.Add(
                    new RawMaterialReleaseReportItemDto
                    {
                        TransactionId =
                            transaction.transaction_id,

                        MaterialId =
                            record.Material.material_id,

                        MaterialCode =
                            record.Material.material_code,

                        MaterialName =
                            record.Material.material_name,

                        CategoryName =
                            record.CategoryName,

                        BranchId =
                            transaction.branch_id,

                        BranchName =
                            record.BranchName,

                        LotNo =
                            lotNo,

                        LotDisplay =
                            !record.Material.is_lot_tracked
                            || isInternalNonLot
                                ? "Not Lot Tracked"
                                : lotNo,

                        QuantityReleased =
                            quantityReleased,

                        Uom =
                            transaction.uom,

                        RequisitionId =
                            transaction.reference_id,

                        RequisitionNo =
                            transaction.reference_no
                            ?? "",

                        ReleasedBy =
                            requisition?.ReleasedBy
                            ?? "",

                        ReleasedByName =
                            GetUserName(
                                requisition?.ReleasedBy),

                        ReceivedBy =
                            requisition?.ReceivedBy
                            ?? "",

                        ReceivedByName =
                            GetUserName(
                                requisition?.ReceivedBy),

                        VerifiedBy =
                            requisition?.VerifiedBy
                            ?? "",

                        VerifiedByName =
                            GetUserName(
                                requisition?.VerifiedBy),

                        PostedBy =
                            requisition?.PostedBy
                            ?? transaction.encoded_by
                            ?? "",

                        PostedByName =
                            GetUserName(
                                requisition?.PostedBy
                                ?? transaction.encoded_by),

                        TransactionDate =
                            transaction.transaction_date,

                        TimeServed =
                            requisition?.TimeServed,

                        Remarks =
                            transaction.remarks
                            ?? ""
                    }
                );
            }

            // =====================================================
            // SUMMARY
            // =====================================================

            var summary =
                new RawMaterialReleaseSummaryDto
                {
                    TotalMrsReleased =
                        items
                            .Where(x =>
                                x.RequisitionId.HasValue)
                            .Select(x =>
                                x.RequisitionId)
                            .Distinct()
                            .Count(),

                    TotalReleaseTransactions =
                        items.Count,

                    UniqueMaterials =
                        items
                            .Select(x =>
                                x.MaterialId)
                            .Distinct()
                            .Count(),

                    TotalBranches =
                        items
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(
                                    x.BranchId))
                            .Select(x =>
                                x.BranchId)
                            .Distinct()
                            .Count()
                };

            // =====================================================
            // TOTALS BY UOM
            // Do NOT combine KG + L + PCS, etc.
            // =====================================================

            var releasedByUom =
                items
                    .GroupBy(x =>
                        string.IsNullOrWhiteSpace(x.Uom)
                            ? "N/A"
                            : x.Uom.Trim().ToUpperInvariant())
                    .Select(x =>
                        new RawMaterialReleaseByUomDto
                        {
                            Uom =
                                x.Key,

                            Quantity =
                                x.Sum(y =>
                                    y.QuantityReleased)
                        })
                    .OrderBy(x =>
                        x.Uom)
                    .ToList();

            return
                new RawMaterialReleaseReportResponseDto
                {
                    Summary =
                        summary,

                    ReleasedByUom =
                        releasedByUom,

                    Items =
                        items
                };
        }

        public async Task<RawMaterialAgingReportResponseDto>
    GetAgingReportAsync(
        RawMaterialAgingReportFilterDto filter)
        {
            filter ??=
                new RawMaterialAgingReportFilterDto();

            var today =
                DateTime.Today;


            // ============================================================
            // CURRENT RAW MATERIAL LOTS
            // ============================================================

            var lotQuery =
                from lot in
                    _context.MaterialLotNumbers.AsNoTracking()

                join material in
                    _context.Materials.AsNoTracking()
                    on lot.material_id
                    equals material.material_id

                join category in
                    _context.MaterialCategories.AsNoTracking()
                    on material.material_category_id
                    equals category.material_category_id
                    into categoryJoin

                from category in
                    categoryJoin.DefaultIfEmpty()

                join branch in
                    _context.Branches.AsNoTracking()
                    on lot.branch_id
                    equals branch.branch_id
                    into branchJoin

                from branch in
                    branchJoin.DefaultIfEmpty()

                join supplier in
                    _context.Suppliers.AsNoTracking()
                    on lot.supplier_id
                    equals supplier.SupplierId
                    into supplierJoin

                from supplier in
                    supplierJoin.DefaultIfEmpty()

                where
                    lot.is_active &&
                    lot.quantity > 0 &&
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


            // ============================================================
            // FILTERS THAT CAN BE DONE IN SQL
            // ============================================================

            if (!string.IsNullOrWhiteSpace(
                filter.BranchId))
            {
                var branchId =
                    filter.BranchId.Trim();

                lotQuery =
                    lotQuery.Where(x =>
                        x.Lot.branch_id ==
                        branchId);
            }


            if (filter.CategoryId.HasValue)
            {
                lotQuery =
                    lotQuery.Where(x =>
                        x.Material.material_category_id ==
                        filter.CategoryId.Value);
            }


            if (!string.IsNullOrWhiteSpace(
                filter.Search))
            {
                var search =
                    filter.Search
                        .Trim()
                        .ToLower();

                lotQuery =
                    lotQuery.Where(x =>
                        x.Material.material_code
                            .ToLower()
                            .Contains(search)
                        ||
                        x.Material.material_name
                            .ToLower()
                            .Contains(search)
                        ||
                        x.Lot.lot_no
                            .ToLower()
                            .Contains(search));
            }


            var lots =
                await lotQuery.ToListAsync();


            if (lots.Count == 0)
            {
                return new RawMaterialAgingReportResponseDto();
            }


            // ============================================================
            // TRANSACTIONS
            // ============================================================

            var materialIds =
                lots
                    .Select(x =>
                        x.Material.material_id)
                    .Distinct()
                    .ToList();


            var transactions =
                await _context
                    .MaterialInventoryTransactions
                    .AsNoTracking()
                    .Where(x =>
                        materialIds.Contains(
                            x.material_id))
                    .Select(x => new
                    {
                        x.transaction_id,
                        x.material_id,
                        x.branch_id,
                        x.lot_no,
                        x.transaction_type,
                        x.quantity,
                        x.transaction_date
                    })
                    .ToListAsync();


            // ============================================================
            // BUILD REPORT
            // ============================================================

            var result =
                new List<RawMaterialAgingReportItemDto>();


            foreach (var record in lots)
            {
                var lot =
                    record.Lot;

                var material =
                    record.Material;


                var lotNo =
                    lot.lot_no?.Trim()
                    ?? string.Empty;


                var lotTransactions =
                    transactions
                        .Where(x =>
                            x.material_id ==
                                material.material_id
                            &&
                            string.Equals(
                                x.branch_id?.Trim(),
                                lot.branch_id?.Trim(),
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            string.Equals(
                                x.lot_no?.Trim() ?? "",
                                lotNo,
                                StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(x =>
                            x.transaction_date)
                        .ThenByDescending(x =>
                            x.transaction_id)
                        .ToList();


                // ========================================================
                // FIRST MOVEMENT / DATE IN
                // ========================================================

                var firstTransaction =
                    lotTransactions
                        .OrderBy(x =>
                            x.transaction_date)
                        .ThenBy(x =>
                            x.transaction_id)
                        .FirstOrDefault();


                DateTime? dateIn =
                    firstTransaction
                        ?.transaction_date
                    ??
                    lot.created_at;


                // ========================================================
                // LAST MOVEMENT
                // ========================================================

                var lastTransaction =
                    lotTransactions
                        .FirstOrDefault();


                DateTime? lastMovementDate =
                    lastTransaction
                        ?.transaction_date;


                string lastMovementType =
                    lastTransaction
                        ?.transaction_type
                    ??
                    "";


                string lastMovement =
                    string.IsNullOrWhiteSpace(
                        lastMovementType)
                        ? ""
                        : IsOutboundTransaction(
                            lastMovementType)
                            ? "OUT"
                            : "IN";


                // ========================================================
                // LAST MATERIAL RELEASE
                // ========================================================

                var lastRelease =
                    lotTransactions
                        .Where(x =>
                            string.Equals(
                                x.transaction_type,
                                "MATERIAL_RELEASE",
                                StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(x =>
                            x.transaction_date)
                        .ThenByDescending(x =>
                            x.transaction_id)
                        .FirstOrDefault();


                DateTime? lastReleaseDate =
                    lastRelease
                        ?.transaction_date;


                // ========================================================
                // DAYS
                // ========================================================

                int daysInInventory =
                    dateIn.HasValue
                        ? Math.Max(
                            0,
                            (today -
                             dateIn.Value.Date).Days)
                        : 0;


                int? daysSinceLastMovement =
                    lastMovementDate.HasValue
                        ? Math.Max(
                            0,
                            (today -
                             lastMovementDate.Value.Date).Days)
                        : null;


                int? daysSinceLastRelease =
                    lastReleaseDate.HasValue
                        ? Math.Max(
                            0,
                            (today -
                             lastReleaseDate.Value.Date).Days)
                        : null;


                // ========================================================
                // EXPIRY
                // ========================================================

                int? daysToExpiry =
                    lot.expiration_date.HasValue
                        ? (lot.expiration_date.Value.Date -
                           today).Days
                        : null;


                string expiryStatus;


                if (!daysToExpiry.HasValue)
                {
                    expiryStatus =
                        "NO_EXPIRATION";
                }
                else if (daysToExpiry.Value < 0)
                {
                    expiryStatus =
                        "EXPIRED";
                }
                else if (daysToExpiry.Value <= 30)
                {
                    expiryStatus =
                        "NEAR_EXPIRY";
                }
                else
                {
                    expiryStatus =
                        "GOOD";
                }


                // ========================================================
                // MOVEMENT AGING
                // ========================================================

                int idleDays =
                    daysSinceLastMovement
                    ??
                    daysInInventory;


                string movementStatus;


                if (idleDays <= 30)
                {
                    movementStatus =
                        "ACTIVE";
                }
                else if (idleDays <= 60)
                {
                    movementStatus =
                        "SLOW_MOVING";
                }
                else if (idleDays <= 90)
                {
                    movementStatus =
                        "VERY_SLOW";
                }
                else
                {
                    movementStatus =
                        "NON_MOVING";
                }


                // ========================================================
                // NON-LOT DISPLAY
                // ========================================================

                bool isInternalNonLot =
                    lotNo.StartsWith(
                        "NON-LOT",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    lotNo.StartsWith(
                        "NOLOT",
                        StringComparison.OrdinalIgnoreCase);


                string lotDisplay =
                    !material.is_lot_tracked ||
                    isInternalNonLot
                        ? "Not Lot Tracked"
                        : lotNo;


                result.Add(
                    new RawMaterialAgingReportItemDto
                    {
                        MaterialLotId =
                            lot.material_lot_id,

                        MaterialId =
                            material.material_id,

                        MaterialCode =
                            material.material_code,

                        MaterialName =
                            material.material_name,

                        CategoryId =
                            material.material_category_id,

                        CategoryName =
                            record.Category
                                ?.category_name
                            ??
                            "Uncategorized",

                        BranchId =
                            lot.branch_id,

                        BranchName =
                            record.Branch
                                ?.branch_name
                            ??
                            lot.branch_id,

                        IsLotTracked =
                            material.is_lot_tracked,

                        LotNo =
                            lotNo,

                        LotDisplay =
                            lotDisplay,

                        Quantity =
                            lot.quantity,

                        Uom =
                            string.IsNullOrWhiteSpace(
                                lot.uom)
                                ? material.uom
                                : lot.uom,

                        DateIn =
                            dateIn,

                        LastReleaseDate =
                            lastReleaseDate,

                        LastMovementDate =
                            lastMovementDate,

                        LastMovementType =
                            lastMovementType,

                        LastMovement =
                            lastMovement,

                        DaysInInventory =
                            daysInInventory,

                        DaysSinceLastMovement =
                            daysSinceLastMovement,

                        DaysSinceLastRelease =
                            daysSinceLastRelease,

                        ManufacturingDate =
                            lot.manufacturing_date,

                        ExpirationDate =
                            lot.expiration_date,

                        DaysToExpiry =
                            daysToExpiry,

                        MovementStatus =
                            movementStatus,

                        ExpiryStatus =
                            expiryStatus,

                        SupplierName =
                            record.Supplier
                                ?.SupplierName
                            ??
                            "Not Specified"
                    });
            }


            // ============================================================
            // COMPUTED FILTERS
            // ============================================================

            if (!string.IsNullOrWhiteSpace(
                filter.MovementStatus))
            {
                var status =
                    filter.MovementStatus
                        .Trim()
                        .ToUpperInvariant();

                result =
                    result
                        .Where(x =>
                            x.MovementStatus ==
                            status)
                        .ToList();
            }


            if (filter.MinimumDaysIdle.HasValue)
            {
                result =
                    result
                        .Where(x =>
                            (x.DaysSinceLastMovement
                             ?? x.DaysInInventory)
                            >=
                            filter.MinimumDaysIdle.Value)
                        .ToList();
            }


            if (filter.MaximumDaysIdle.HasValue)
            {
                result =
                    result
                        .Where(x =>
                            (x.DaysSinceLastMovement
                             ?? x.DaysInInventory)
                            <=
                            filter.MaximumDaysIdle.Value)
                        .ToList();
            }


            // ============================================================
            // SORT
            // Oldest / most idle first
            // ============================================================

            result =
                result
                    .OrderByDescending(x =>
                        x.DaysSinceLastMovement
                        ??
                        x.DaysInInventory)
                    .ThenBy(x =>
                        x.MaterialName)
                    .ThenBy(x =>
                        x.LotDisplay)
                    .ToList();


            // ============================================================
            // RESPONSE
            // ============================================================

            return new RawMaterialAgingReportResponseDto
            {
                Summary =
                    new RawMaterialAgingReportSummaryDto
                    {
                        TotalLots =
                            result.Count,

                        Active =
                            result.Count(x =>
                                x.MovementStatus ==
                                "ACTIVE"),

                        SlowMoving =
                            result.Count(x =>
                                x.MovementStatus ==
                                "SLOW_MOVING" ||
                                x.MovementStatus ==
                                "VERY_SLOW"),

                        NonMoving =
                            result.Count(x =>
                                x.MovementStatus ==
                                "NON_MOVING"),

                        NearExpiry =
                            result.Count(x =>
                                x.ExpiryStatus ==
                                "NEAR_EXPIRY"),

                        Expired =
                            result.Count(x =>
                                x.ExpiryStatus ==
                                "EXPIRED")
                    },

                Items =
                    result
            };
        }

        private static bool IsOutboundTransaction(
    string? transactionType)
        {
            var type =
                transactionType?
                    .Trim()
                    .ToUpperInvariant()
                ?? string.Empty;

            return
                type.Contains("ISSUE") ||
                type.Contains("OUT") ||
                type.Contains("RELEASE") ||
                type.Contains("CONSUME") ||
                type.Contains("USAGE") ||
                type.Contains("TRANSFER_OUT") ||
                type.Contains("ADJUSTMENT_OUT");
        }


        public async Task<RawMaterialUsageTrendResponseDto>
    GetUsageTrendReportAsync(
        RawMaterialUsageTrendFilterDto filter)
        {
            filter ??=
                new RawMaterialUsageTrendFilterDto();


            // ============================================================
            // DATE RANGE
            //
            // Default = last 30 days including today
            // ============================================================

            var toDate =
                filter.ToDate?.Date
                ?? DateTime.Today;


            var fromDate =
                filter.FromDate?.Date
                ?? toDate.AddDays(-29);


            if (fromDate > toDate)
            {
                throw new InvalidOperationException(
                    "From date cannot be later than To date."
                );
            }


            var toExclusive =
                toDate.AddDays(1);


            int totalDays =
                (toDate - fromDate).Days + 1;


            // ============================================================
            // MATERIAL RELEASE TRANSACTIONS
            // ============================================================

            var query =
                from transaction in
                    _context.MaterialInventoryTransactions
                        .AsNoTracking()

                join material in
                    _context.Materials
                        .AsNoTracking()
                    on transaction.material_id
                    equals material.material_id

                join category in
                    _context.MaterialCategories
                        .AsNoTracking()
                    on material.material_category_id
                    equals category.material_category_id
                    into categoryJoin

                from category in
                    categoryJoin.DefaultIfEmpty()

                join branch in
                    _context.Branches
                        .AsNoTracking()
                    on transaction.branch_id
                    equals branch.branch_id
                    into branchJoin

                from branch in
                    branchJoin.DefaultIfEmpty()

                where
                    transaction.transaction_type ==
                        "MATERIAL_RELEASE"
                    &&
                    transaction.transaction_date >=
                        fromDate
                    &&
                    transaction.transaction_date <
                        toExclusive
                    &&
                    material.is_active
                    &&
                    !material.is_deleted

                select new
                {
                    Transaction = transaction,
                    Material = material,

                    CategoryName =
                        category != null
                            ? category.category_name
                            : "Uncategorized",

                    BranchName =
                        branch != null
                            ? branch.branch_name
                            : transaction.branch_id
                };


            // ============================================================
            // FILTERS
            // ============================================================

            if (!string.IsNullOrWhiteSpace(
                filter.BranchId))
            {
                var branchId =
                    filter.BranchId.Trim();

                query =
                    query.Where(x =>
                        x.Transaction.branch_id ==
                        branchId);
            }


            if (filter.MaterialId.HasValue)
            {
                query =
                    query.Where(x =>
                        x.Transaction.material_id ==
                        filter.MaterialId.Value);
            }


            if (filter.CategoryId.HasValue)
            {
                query =
                    query.Where(x =>
                        x.Material.material_category_id ==
                        filter.CategoryId.Value);
            }


            if (!string.IsNullOrWhiteSpace(
                filter.Search))
            {
                var search =
                    filter.Search
                        .Trim()
                        .ToLowerInvariant();

                query =
                    query.Where(x =>
                        x.Material.material_code
                            .ToLower()
                            .Contains(search)

                        ||

                        x.Material.material_name
                            .ToLower()
                            .Contains(search)

                        ||

                        (
                            x.Transaction.lot_no != null
                            &&
                            x.Transaction.lot_no
                                .ToLower()
                                .Contains(search)
                        ));
            }


            var records =
                await query
                    .OrderBy(x =>
                        x.Transaction.transaction_date)
                    .ThenBy(x =>
                        x.Transaction.transaction_id)
                    .ToListAsync();


            // ============================================================
            // DAILY TREND
            // ============================================================

            var dailyTrend =
                records
                    .GroupBy(x =>
                        x.Transaction
                            .transaction_date.Date)
                    .Select(day =>
                        new RawMaterialUsageTrendDailyDto
                        {
                            Date =
                                day.Key,

                            TransactionCount =
                                day.Count(),

                            UsageByUom =
                                day
                                    .GroupBy(x =>
                                        string.IsNullOrWhiteSpace(
                                            x.Transaction.uom)
                                            ? "N/A"
                                            : x.Transaction.uom
                                                .Trim()
                                                .ToUpperInvariant())
                                    .Select(uom =>
                                        new RawMaterialUsageTrendByUomDto
                                        {
                                            Uom =
                                                uom.Key,

                                            Quantity =
                                                uom.Sum(x =>
                                                    Math.Abs(
                                                        x.Transaction.quantity))
                                        })
                                    .OrderBy(x =>
                                        x.Uom)
                                    .ToList()
                        })
                    .OrderBy(x =>
                        x.Date)
                    .ToList();


            // ============================================================
            // MATERIAL USAGE SUMMARY
            // ============================================================

            var materials =
                records
                    .GroupBy(x =>
                        new
                        {
                            x.Material.material_id,
                            x.Material.material_code,
                            x.Material.material_name,
                            x.CategoryName,

                            Uom =
                                string.IsNullOrWhiteSpace(
                                    x.Transaction.uom)
                                    ? x.Material.uom
                                    : x.Transaction.uom
                        })
                    .Select(group =>
                    {
                        decimal totalUsage =
                            group.Sum(x =>
                                Math.Abs(
                                    x.Transaction.quantity));


                        int activeUsageDays =
                            group
                                .Select(x =>
                                    x.Transaction
                                        .transaction_date.Date)
                                .Distinct()
                                .Count();


                        decimal averageDailyUsage =
                            totalDays > 0
                                ? totalUsage /
                                  totalDays
                                : 0m;


                        decimal averagePerActiveDay =
                            activeUsageDays > 0
                                ? totalUsage /
                                  activeUsageDays
                                : 0m;


                        return new RawMaterialUsageTrendMaterialDto
                        {
                            MaterialId =
                                group.Key.material_id,

                            MaterialCode =
                                group.Key.material_code,

                            MaterialName =
                                group.Key.material_name,

                            CategoryName =
                                group.Key.CategoryName,

                            Uom =
                                group.Key.Uom,

                            TotalUsage =
                                totalUsage,

                            AverageDailyUsage =
                                averageDailyUsage,

                            AverageUsagePerActiveDay =
                                averagePerActiveDay,

                            ActiveUsageDays =
                                activeUsageDays,

                            ReleaseTransactions =
                                group.Count(),

                            FirstUsageDate =
                                group.Min(x =>
                                    x.Transaction.transaction_date),

                            LastUsageDate =
                                group.Max(x =>
                                    x.Transaction.transaction_date)
                        };
                    })
                    .OrderByDescending(x =>
                        x.TotalUsage)
                    .ThenBy(x =>
                        x.MaterialName)
                    .ToList();


            // ============================================================
            // SUMMARY
            // ============================================================

            var summary =
                new RawMaterialUsageTrendSummaryDto
                {
                    FromDate =
                        fromDate,

                    ToDate =
                        toDate,

                    TotalDays =
                        totalDays,

                    ReleaseTransactions =
                        records.Count,

                    UniqueMaterials =
                        records
                            .Select(x =>
                                x.Material.material_id)
                            .Distinct()
                            .Count(),

                    ActiveUsageDays =
                        records
                            .Select(x =>
                                x.Transaction
                                    .transaction_date.Date)
                            .Distinct()
                            .Count()
                };


            return new RawMaterialUsageTrendResponseDto
            {
                Summary =
                    summary,

                DailyTrend =
                    dailyTrend,

                Materials =
                    materials
            };
        }

        public async Task<RawMaterialForecastResponseDto>
    GetForecastReportAsync(
        RawMaterialForecastFilterDto filter)
        {
            filter ??= new RawMaterialForecastFilterDto();

            filter.UsageHistoryDays =
                Math.Clamp(filter.UsageHistoryDays, 7, 365);

            filter.SafetyStockDays =
                Math.Clamp(filter.SafetyStockDays, 0, 365);

            filter.TargetCoverDays =
                Math.Clamp(filter.TargetCoverDays, 1, 365);

            var today = DateTime.Today;

            var usageFrom =
                today.AddDays(-(filter.UsageHistoryDays - 1));

            var usageToExclusive =
                today.AddDays(1);


            // ============================================================
            // MATERIALS
            // ============================================================

            var materialQuery =
                _context.Materials
                    .AsNoTracking()
                    .Where(x =>
                        x.is_active &&
                        !x.is_deleted);

            if (filter.MaterialId.HasValue)
            {
                materialQuery =
                    materialQuery.Where(x =>
                        x.material_id ==
                        filter.MaterialId.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                materialQuery =
                    materialQuery.Where(x =>
                        x.material_category_id ==
                        filter.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search =
                    filter.Search.Trim().ToLowerInvariant();

                materialQuery =
                    materialQuery.Where(x =>
                        x.material_code.ToLower().Contains(search) ||
                        x.material_name.ToLower().Contains(search));
            }

            var materials =
                await materialQuery
                    .Include(x => x.Category)
                    .ToListAsync();


            var materialIds =
                materials
                    .Select(x => x.material_id)
                    .ToList();


            // ============================================================
            // CURRENT STOCK
            // ============================================================

            var lotQuery =
                _context.MaterialLotNumbers
                    .AsNoTracking()
                    .Where(x =>
                        x.is_active &&
                        materialIds.Contains(x.material_id));

            if (!string.IsNullOrWhiteSpace(filter.BranchId))
            {
                var branchId = filter.BranchId.Trim();

                lotQuery =
                    lotQuery.Where(x =>
                        x.branch_id == branchId);
            }

            var lots =
                await lotQuery.ToListAsync();


            // ============================================================
            // USAGE HISTORY
            // ============================================================

            var usageQuery =
                _context.MaterialInventoryTransactions
                    .AsNoTracking()
                    .Where(x =>
                        materialIds.Contains(x.material_id) &&
                        x.transaction_type == "MATERIAL_RELEASE" &&
                        x.transaction_date >= usageFrom &&
                        x.transaction_date < usageToExclusive);

            if (!string.IsNullOrWhiteSpace(filter.BranchId))
            {
                var branchId = filter.BranchId.Trim();

                usageQuery =
                    usageQuery.Where(x =>
                        x.branch_id == branchId);
            }

            var usages =
                await usageQuery
                    .Select(x => new
                    {
                        x.material_id,
                        x.quantity
                    })
                    .ToListAsync();


            // ============================================================
            // OPEN PO BALANCES
            // ============================================================

            var poLines =
                await (
                    from po in _context.PurchaseOrderHeaders.AsNoTracking()

                    join line in _context.PurchaseOrderLines.AsNoTracking()
                        on po.PoId equals line.PoId

                    where
                        materialIds.Contains(line.MaterialId)
                        &&
                        (
                            po.Status == "APPROVED"
                            ||
                            po.Status == "PARTIALLY_RECEIVED"
                        )
                        &&
                        line.BalanceQty > 0

                    select new
                    {
                        po.PoId,
                        po.SupplierId,
                        line.PoLineId,
                        line.MaterialId,
                        line.QuoteId,
                        line.BalanceQty
                    }
                )
                .ToListAsync();


            // ============================================================
            // SCHEDULES
            // ============================================================

            var poLineIds =
                poLines
                    .Select(x => x.PoLineId)
                    .Distinct()
                    .ToList();

            var scheduleLines =
                await (
                    from schedule in
                        _context.PurchaseOrderDeliverySchedules.AsNoTracking()

                    join line in
                        _context.PurchaseOrderDeliveryScheduleLines.AsNoTracking()
                        on schedule.ScheduleId equals line.ScheduleId

                    where
                        poLineIds.Contains(line.PoLineId)
                        &&
                        line.BalanceQty > 0
                        &&
                        line.Status != "RECEIVED"
                        &&
                        line.Status != "CANCELLED"

                    select new
                    {
                        line.PoLineId,
                        line.BalanceQty,
                        schedule.ScheduledDate
                    }
                )
                .ToListAsync();


            // ============================================================
            // QUOTE LEAD TIME
            // ============================================================

            var quoteIds =
                poLines
                    .Where(x => x.QuoteId > 0)
                    .Select(x => x.QuoteId)
                    .Distinct()
                    .ToList();

            var quotes =
                await _context.PurchasingCanvassQuotes
                    .AsNoTracking()
                    .Where(x =>
                        quoteIds.Contains(x.QuoteId))
                    .Select(x => new
                    {
                        x.QuoteId,
                        x.DeliveryDays
                    })
                    .ToListAsync();


            // ============================================================
            // SUPPLIERS
            // ============================================================

            var supplierIds =
                poLines
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .ToList();

            var suppliers =
                await _context.Suppliers
                    .AsNoTracking()
                    .Where(x =>
                        supplierIds.Contains(x.SupplierId))
                    .Select(x => new
                    {
                        x.SupplierId,
                        x.SupplierName
                    })
                    .ToDictionaryAsync(x => x.SupplierId);


            // ============================================================
            // BUILD
            // ============================================================

            var result =
                new List<RawMaterialForecastItemDto>();


            foreach (var material in materials)
            {
                decimal currentStock =
                    lots
                        .Where(x =>
                            x.material_id ==
                            material.material_id)
                        .Sum(x => x.quantity);


                decimal usageDuringHistory =
                    usages
                        .Where(x =>
                            x.material_id ==
                            material.material_id)
                        .Sum(x =>
                            Math.Abs(x.quantity));


                decimal averageDailyUsage =
                    usageDuringHistory /
                    filter.UsageHistoryDays;


                var materialPoLines =
                    poLines
                        .Where(x =>
                            x.MaterialId ==
                            material.material_id)
                        .ToList();


                decimal incomingPoQty =
                    materialPoLines
                        .Sum(x => x.BalanceQty);


                decimal stockPosition =
                    currentStock +
                    incomingPoQty;


                // Lead time
                var materialQuoteIds =
                    materialPoLines
                        .Select(x => x.QuoteId)
                        .ToList();

                var leadTimes =
                    quotes
                        .Where(x =>
                            materialQuoteIds.Contains(x.QuoteId)
                            &&
                            x.DeliveryDays.HasValue)
                        .Select(x =>
                            x.DeliveryDays!.Value)
                        .ToList();


                int? leadTimeDays =
        leadTimes.Any()
            ? leadTimes.Max()
            : null;

                decimal leadTimeDemand =
                    averageDailyUsage *
                    (leadTimeDays ?? 0);

                decimal safetyStockQty =
                    averageDailyUsage *
                    filter.SafetyStockDays;


                decimal reorderPoint =
                    leadTimeDemand +
                    safetyStockQty;


                decimal? daysCover =
                    averageDailyUsage > 0
                        ? currentStock /
                          averageDailyUsage
                        : null;


                decimal? daysCoverWithIncoming =
                    averageDailyUsage > 0
                        ? stockPosition /
                          averageDailyUsage
                        : null;


                decimal targetStockQty =
                    averageDailyUsage *
                    filter.TargetCoverDays;


                decimal suggestedOrderQty =
                    Math.Max(
                        0m,
                        targetStockQty -
                        stockPosition
                    );


                var relevantPoLineIds =
                    materialPoLines
                        .Select(x => x.PoLineId)
                        .ToList();


                DateTime? nextExpectedDelivery =
                    scheduleLines
                        .Where(x =>
                            relevantPoLineIds.Contains(
                                x.PoLineId))
                        .Select(x =>
                            (DateTime?)x.ScheduledDate)
                        .OrderBy(x => x)
                        .FirstOrDefault();


                string supplierName = "";

                var supplierId =
                    materialPoLines
                        .Select(x => x.SupplierId)
                        .FirstOrDefault();

                if (
                    supplierId > 0 &&
                    suppliers.TryGetValue(
                        supplierId,
                        out var supplier)
                )
                {
                    supplierName =
                        supplier.SupplierName;
                }


                // --------------------------------------------------------
                // STATUS
                // --------------------------------------------------------

                string status;

                if (averageDailyUsage <= 0)
                {
                    status = "NO_USAGE_HISTORY";
                }
                else if (currentStock <= 0)
                {
                    status = incomingPoQty > 0
                        ? "WATCH"
                        : "CRITICAL";
                }
                else if (stockPosition <= reorderPoint)
                {
                    status = incomingPoQty > 0
                        ? "WATCH"
                        : "REORDER";
                }
                else if (suggestedOrderQty > 0)
                {
                    status = "WATCH";
                }
                else
                {
                    status = "HEALTHY";
                }


                result.Add(
                    new RawMaterialForecastItemDto
                    {
                        MaterialId =
                            material.material_id,

                        MaterialCode =
                            material.material_code,

                        MaterialName =
                            material.material_name,

                        CategoryName =
                            material.Category
                                ?.category_name
                            ?? "Uncategorized",

                        Uom =
                            material.uom,

                        CurrentStock =
                            currentStock,

                        IncomingPoQty =
                            incomingPoQty,

                        StockPosition =
                            stockPosition,

                        UsageDuringHistory =
                            usageDuringHistory,

                        AverageDailyUsage =
                            averageDailyUsage,

                        LeadTimeDays =
                            leadTimeDays,

                        LeadTimeDemand =
                            leadTimeDemand,

                        SafetyStockDays =
                            filter.SafetyStockDays,

                        SafetyStockQty =
                            safetyStockQty,

                        ReorderPoint =
                            reorderPoint,

                        DaysCover =
                            daysCover,

                        DaysCoverWithIncoming =
                            daysCoverWithIncoming,

                        TargetCoverDays =
                            filter.TargetCoverDays,

                        TargetStockQty =
                            targetStockQty,

                        SuggestedOrderQty =
                            suggestedOrderQty,

                        NextExpectedDelivery =
                            nextExpectedDelivery,

                        SupplierName =
                            supplierName,

                        Status =
                            status
                    }
                );
            }


            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var status =
                    filter.Status
                        .Trim()
                        .ToUpperInvariant();

                result =
                    result
                        .Where(x =>
                            x.Status == status)
                        .ToList();
            }


            int Priority(string status)
            {
                return status switch
                {
                    "CRITICAL" => 1,
                    "REORDER" => 2,
                    "WATCH" => 3,
                    "HEALTHY" => 4,
                    "NO_USAGE_HISTORY" => 5,
                    _ => 99
                };
            }


            result =
                result
                    .OrderBy(x =>
                        Priority(x.Status))
                    .ThenBy(x =>
                        x.DaysCover
                        ?? decimal.MaxValue)
                    .ToList();


            return new RawMaterialForecastResponseDto
            {
                Summary =
                    new RawMaterialForecastSummaryDto
                    {
                        TotalMaterials =
                            result.Count,

                        Healthy =
                            result.Count(x =>
                                x.Status == "HEALTHY"),

                        Watch =
                            result.Count(x =>
                                x.Status == "WATCH"),

                        Reorder =
                            result.Count(x =>
                                x.Status == "REORDER"),

                        Critical =
                            result.Count(x =>
                                x.Status == "CRITICAL"),

                        NoUsageHistory =
                            result.Count(x =>
                                x.Status ==
                                "NO_USAGE_HISTORY")
                    },

                Items =
                    result
            };
        }
    }
}