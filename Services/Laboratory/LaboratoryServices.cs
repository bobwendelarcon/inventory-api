
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using inventory_api.Data;
using inventory_api.Models.Laboratory;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Laboratory
{
    public class LaboratoryInventoryService
    {
        private readonly AppDbContext _context;

        public LaboratoryInventoryService(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // MASTER DATA - CATEGORY
        // =====================================================

        public async Task<List<LaboratoryCategory>> GetCategoriesAsync()
        {
            return await _context.LaboratoryCategories
                .AsNoTracking()
                .Where(x => x.is_active)
                .OrderBy(x => x.category_name)
                .ToListAsync();
        }

        public async Task<LaboratoryCategory> SaveCategoryAsync(
            LaboratoryCategory model)
        {
            model.category_name = model.category_name.Trim();

            if (string.IsNullOrWhiteSpace(model.category_name))
                throw new ArgumentException("Category name is required.");

            var exists = await _context.LaboratoryCategories
                .AnyAsync(x =>
                    x.category_name == model.category_name &&
                    x.category_id != model.category_id);

            if (exists)
                throw new ArgumentException("Category already exists.");

            if (model.category_id == 0)
            {
                var category = new LaboratoryCategory
                {
                    category_name = model.category_name,
                    is_active = true
                };

                _context.LaboratoryCategories.Add(category);
                await _context.SaveChangesAsync();
                return category;
            }

            var existing = await _context.LaboratoryCategories
                .FindAsync(model.category_id);

            if (existing == null)
                throw new KeyNotFoundException("Category not found.");

            existing.category_name = model.category_name;
            existing.is_active = model.is_active;

            await _context.SaveChangesAsync();
            return existing;
        }

        // =====================================================
        // MASTER DATA - BRAND
        // =====================================================

        public async Task<List<LaboratoryBrand>> GetBrandsAsync()
        {
            return await _context.LaboratoryBrands
                .AsNoTracking()
                .Where(x => x.is_active)
                .OrderBy(x => x.brand_name)
                .ToListAsync();
        }

        public async Task<LaboratoryBrand> SaveBrandAsync(
            LaboratoryBrand model)
        {
            model.brand_name = model.brand_name.Trim();

            if (string.IsNullOrWhiteSpace(model.brand_name))
                throw new ArgumentException("Brand name is required.");

            var exists = await _context.LaboratoryBrands
                .AnyAsync(x =>
                    x.brand_name == model.brand_name &&
                    x.brand_id != model.brand_id);

            if (exists)
                throw new ArgumentException("Brand already exists.");

            if (model.brand_id == 0)
            {
                var brand = new LaboratoryBrand
                {
                    brand_name = model.brand_name,
                    is_active = true
                };

                _context.LaboratoryBrands.Add(brand);
                await _context.SaveChangesAsync();
                return brand;
            }

            var existing = await _context.LaboratoryBrands
                .FindAsync(model.brand_id);

            if (existing == null)
                throw new KeyNotFoundException("Brand not found.");

            existing.brand_name = model.brand_name;
            existing.is_active = model.is_active;

            await _context.SaveChangesAsync();
            return existing;
        }

        // =====================================================
        // MASTER DATA - LOCATION
        // =====================================================

        public async Task<List<LaboratoryLocation>> GetLocationsAsync()
        {
            return await _context.LaboratoryLocations
                .AsNoTracking()
                .Where(x => x.is_active)
                .OrderBy(x => x.location_name)
                .ToListAsync();
        }

        public async Task<LaboratoryLocation> SaveLocationAsync(
            LaboratoryLocation model)
        {
            model.location_name = model.location_name.Trim();

            if (string.IsNullOrWhiteSpace(model.location_name))
                throw new ArgumentException("Location name is required.");

            var exists = await _context.LaboratoryLocations
                .AnyAsync(x =>
                    x.location_name == model.location_name &&
                    x.location_id != model.location_id);

            if (exists)
                throw new ArgumentException("Location already exists.");

            if (model.location_id == 0)
            {
                var location = new LaboratoryLocation
                {
                    location_name = model.location_name,
                    description = model.description,
                    is_active = true
                };

                _context.LaboratoryLocations.Add(location);
                await _context.SaveChangesAsync();
                return location;
            }

            var existing = await _context.LaboratoryLocations
                .FindAsync(model.location_id);

            if (existing == null)
                throw new KeyNotFoundException("Location not found.");

            existing.location_name = model.location_name;
            existing.description = model.description;
            existing.is_active = model.is_active;

            await _context.SaveChangesAsync();
            return existing;
        }

        // =====================================================
        // MASTER DATA - ITEMS
        // =====================================================

        public async Task<object> GetItemsAsync(string search = "")
        {
            var query =
                from item in _context.LaboratoryItems.AsNoTracking()
                join category in _context.LaboratoryCategories
                    on item.category_id equals category.category_id
                join brandRow in _context.LaboratoryBrands
                    on item.brand_id equals (long?)brandRow.brand_id
                    into brands
                from brand in brands.DefaultIfEmpty()
                where item.is_active
                select new
                {
                    item.item_id,
                    item.item_code,
                    item.item_description,
                    item.category_id,
                    category_name = category.category_name,
                    item.brand_id,
                    brand_name = brand != null
                        ? brand.brand_name : "",
                    item.uom,
                    item.minimum_stock,
                    item.tracking_type,
                    item.is_active,
                    item.created_at
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(x =>
                    x.item_code.Contains(keyword) ||
                    x.item_description.Contains(keyword) ||
                    x.category_name.Contains(keyword) ||
                    x.brand_name.Contains(keyword));
            }

            return await query
                .OrderBy(x => x.item_description)
                .ToListAsync();
        }

        public async Task<LaboratoryItem> SaveItemAsync(
            LaboratoryItem model)
        {
            model.item_code = model.item_code.Trim();
            model.item_description = model.item_description.Trim();
            model.uom = model.uom.Trim();
            model.tracking_type =
                model.tracking_type.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(model.item_code))
                throw new ArgumentException("Item code is required.");

            if (string.IsNullOrWhiteSpace(model.item_description))
                throw new ArgumentException("Item description is required.");

            if (string.IsNullOrWhiteSpace(model.uom))
                throw new ArgumentException("Unit is required.");

            if (model.minimum_stock < 0)
                throw new ArgumentException(
                    "Minimum stock cannot be negative.");

            if (!new[] { "NONE", "LOT", "SERIAL" }
                    .Contains(model.tracking_type))
                throw new ArgumentException(
                    "Tracking type must be NONE, LOT, or SERIAL.");

            var categoryExists = await _context.LaboratoryCategories
                .AnyAsync(x =>
                    x.category_id == model.category_id &&
                    x.is_active);

            if (!categoryExists)
                throw new ArgumentException("Invalid category.");

            if (model.brand_id.HasValue)
            {
                var brandExists = await _context.LaboratoryBrands
                    .AnyAsync(x =>
                        x.brand_id == model.brand_id.Value &&
                        x.is_active);

                if (!brandExists)
                    throw new ArgumentException("Invalid brand.");
            }

            var codeExists = await _context.LaboratoryItems
                .AnyAsync(x =>
                    x.item_code == model.item_code &&
                    x.item_id != model.item_id);

            if (codeExists)
                throw new ArgumentException("Item code already exists.");

            if (model.item_id == 0)
            {
                var item = new LaboratoryItem
                {
                    item_code = model.item_code,
                    item_description = model.item_description,
                    category_id = model.category_id,
                    brand_id = model.brand_id,
                    uom = model.uom,
                    minimum_stock = model.minimum_stock,
                    tracking_type = model.tracking_type,
                    is_active = true,
                    created_at = DateTime.UtcNow
                };

                _context.LaboratoryItems.Add(item);
                await _context.SaveChangesAsync();
                return item;
            }

            var existing = await _context.LaboratoryItems
                .FindAsync(model.item_id);

            if (existing == null)
                throw new KeyNotFoundException("Item not found.");

            // Tracking changes can invalidate existing stock identities.
            if (existing.tracking_type != model.tracking_type)
            {
                var hasInventory = await _context.LaboratoryInventories
                    .AnyAsync(x => x.item_id == model.item_id);

                if (hasInventory)
                    throw new ArgumentException(
                        "Tracking type cannot be changed after " +
                        "inventory records have been created.");
            }

            existing.item_code = model.item_code;
            existing.item_description = model.item_description;
            existing.category_id = model.category_id;
            existing.brand_id = model.brand_id;
            existing.uom = model.uom;
            existing.minimum_stock = model.minimum_stock;
            existing.tracking_type = model.tracking_type;
            existing.is_active = model.is_active;

            await _context.SaveChangesAsync();
            return existing;
        }


        // =====================================================
        // DEACTIVATE LABORATORY ITEM (SOFT DELETE)
        // =====================================================

        public async Task<bool> DeactivateItemAsync(long itemId)
        {
            if (itemId <= 0)
                throw new ArgumentException("Invalid laboratory item ID.");

            var item = await _context.LaboratoryItems
                .FirstOrDefaultAsync(x => x.item_id == itemId);

            if (item == null)
                throw new KeyNotFoundException("Laboratory item not found.");

            if (!item.is_active)
                throw new ArgumentException(
                    "This laboratory item is already inactive.");

            // Do not allow deletion if inventory records exist,
            // even if their quantity is zero.
            var hasInventory = await _context.LaboratoryInventories
                .AnyAsync(x => x.item_id == itemId);

            if (hasInventory)
                throw new ArgumentException(
                    "Cannot delete this item because it already has " +
                    "inventory records. Existing inventory and " +
                    "transaction history must be preserved.");

            // Extra protection for historical transactions.
            var hasTransactions = await _context.LaboratoryInventoryTransactions
                .AnyAsync(x => x.item_id == itemId);

            if (hasTransactions)
                throw new ArgumentException(
                    "Cannot delete this item because it has " +
                    "transaction history.");

            // Soft delete only.
            item.is_active = false;

            await _context.SaveChangesAsync();

            return true;
        }


        // =====================================================
        // INVENTORY LIST
        // =====================================================

        public async Task<object> GetInventoryAsync(
            string search = "",
            long? categoryId = null,
            long? brandId = null,
            long? locationId = null,
            string stockStatus = "")
        {
            var query =
                from inv in _context.LaboratoryInventories.AsNoTracking()
                join item in _context.LaboratoryItems
                    on inv.item_id equals item.item_id
                join category in _context.LaboratoryCategories
                    on item.category_id equals category.category_id
                join location in _context.LaboratoryLocations
                    on inv.location_id equals location.location_id
                join brandRow in _context.LaboratoryBrands
                    on item.brand_id equals (long?)brandRow.brand_id
                    into brands
                from brand in brands.DefaultIfEmpty()
                where item.is_active
                select new
                {
                    inv.inventory_id,
                    inv.item_id,
                    item.item_code,
                    item.item_description,
                    item.category_id,
                    category_name = category.category_name,
                    item.brand_id,
                    brand_name = brand != null
                        ? brand.brand_name : "",
                    inv.serial_lot_no,
                    item.uom,
                    inv.quantity,
                    item.minimum_stock,
                    inv.location_id,
                    location_name = location.location_name,
                    inv.item_status,
                    inv.date_acquired,
                    inv.expiry_date,
                    inv.supplier_name,
                    inv.remarks,
                    item.tracking_type
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(x =>
                    x.item_code.Contains(keyword) ||
                    x.item_description.Contains(keyword) ||
                    x.category_name.Contains(keyword) ||
                    x.brand_name.Contains(keyword) ||
                    (x.serial_lot_no != null &&
                     x.serial_lot_no.Contains(keyword)));
            }

            if (categoryId.HasValue)
                query = query.Where(x =>
                    x.category_id == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(x =>
                    x.brand_id == brandId.Value);

            if (locationId.HasValue)
                query = query.Where(x =>
                    x.location_id == locationId.Value);

            var rows = await query.ToListAsync();

            var result = rows.Select(x => new
            {
                x.inventory_id,
                x.item_id,
                x.item_code,
                x.item_description,
                x.category_id,
                x.category_name,
                x.brand_id,
                x.brand_name,
                x.serial_lot_no,
                x.uom,
                x.quantity,
                x.minimum_stock,
                x.location_id,
                x.location_name,
                x.item_status,
                x.date_acquired,
                x.expiry_date,
                x.supplier_name,
                x.remarks,
                x.tracking_type,
                stock_status = x.quantity <= 0
                    ? "OUT OF STOCK"
                    : x.quantity <= x.minimum_stock
                        ? "LOW STOCK"
                        : "NORMAL"
            });

            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                var status = stockStatus.Trim().ToUpperInvariant();
                result = result.Where(x => x.stock_status == status);
            }

            return result
                .OrderBy(x => x.item_description)
                .ThenBy(x => x.location_name)
                .ThenBy(x => x.serial_lot_no)
                .ToList();
        }

        // =====================================================
        // CREATE INVENTORY RECORD
        // Initial quantity is always zero.
        // Opening stock must be recorded using Stock IN.
        // =====================================================

        public async Task<LaboratoryInventory> CreateInventoryAsync(
            LaboratoryInventory model)
        {
            var item = await _context.LaboratoryItems
                .FirstOrDefaultAsync(x =>
                    x.item_id == model.item_id && x.is_active);

            if (item == null)
                throw new ArgumentException("Invalid laboratory item.");

            var locationExists = await _context.LaboratoryLocations
                .AnyAsync(x =>
                    x.location_id == model.location_id &&
                    x.is_active);

            if (!locationExists)
                throw new ArgumentException("Invalid laboratory location.");

            var serialLot = NormalizeSerialLot(
                item.tracking_type, model.serial_lot_no);

            if (item.tracking_type == "SERIAL")
            {
                var duplicateSerial = await _context.LaboratoryInventories
                    .AnyAsync(x =>
                        x.item_id == model.item_id &&
                        x.serial_lot_no == serialLot);

                if (duplicateSerial)
                    throw new ArgumentException(
                        "This serial number already exists for the item.");
            }
            else
            {
                var duplicate = await _context.LaboratoryInventories
                    .AnyAsync(x =>
                        x.item_id == model.item_id &&
                        x.location_id == model.location_id &&
                        x.serial_lot_no == serialLot);

                if (duplicate)
                    throw new ArgumentException(
                        "Inventory record already exists.");
            }

            var now = DateTime.UtcNow;

            var inventory = new LaboratoryInventory
            {
                item_id = model.item_id,
                location_id = model.location_id,
                serial_lot_no = serialLot,
                quantity = 0,
                item_status = string.IsNullOrWhiteSpace(model.item_status)
                    ? "Functional" : model.item_status.Trim(),
                date_acquired = model.date_acquired,
                expiry_date = model.expiry_date,
                supplier_name = model.supplier_name,
                remarks = model.remarks,
                created_at = now,
                updated_at = now
            };

            _context.LaboratoryInventories.Add(inventory);
            await _context.SaveChangesAsync();

            return inventory;
        }

        // =====================================================
        // UPDATE INVENTORY DETAILS
        // Quantity cannot be edited here.
        // =====================================================

        public async Task<LaboratoryInventory> UpdateInventoryDetailsAsync(
            long inventoryId,
            LaboratoryInventory model)
        {
            var inventory = await _context.LaboratoryInventories
                .FindAsync(inventoryId);

            if (inventory == null)
                throw new KeyNotFoundException("Inventory not found.");

            // Item, location and serial/lot identity are immutable here.
            // Use a dedicated transfer/correction workflow if needed.

            inventory.item_status =
                string.IsNullOrWhiteSpace(model.item_status)
                    ? inventory.item_status
                    : model.item_status.Trim();

            inventory.date_acquired = model.date_acquired;
            inventory.expiry_date = model.expiry_date;
            inventory.supplier_name = model.supplier_name;
            inventory.remarks = model.remarks;
            inventory.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return inventory;
        }

        // =====================================================
        // STOCK IN / STOCK OUT / ADJUSTMENT
        // =====================================================

        public async Task<LaboratoryInventoryTransaction> StockInAsync(
            LaboratoryMovementRequest request)
        {
            return await ProcessMovementAsync(request, "IN");
        }

        public async Task<LaboratoryInventoryTransaction> StockOutAsync(
            LaboratoryMovementRequest request)
        {
            return await ProcessMovementAsync(request, "OUT");
        }

        public async Task<LaboratoryInventoryTransaction> AdjustAsync(
            LaboratoryMovementRequest request)
        {
            return await ProcessMovementAsync(request, "ADJUSTMENT");
        }

        private async Task<LaboratoryInventoryTransaction>
            ProcessMovementAsync(
                LaboratoryMovementRequest request,
                string operation)
        {
            if (request.inventory_id <= 0)
                throw new ArgumentException("Inventory ID is required.");

            if (request.quantity < 0)
                throw new ArgumentException(
                    "Quantity cannot be negative.");

            if (string.IsNullOrWhiteSpace(request.performed_by))
                throw new ArgumentException(
                    "Performed by is required.");

            string adjustmentType = "";

            if (operation == "ADJUSTMENT")
            {
                adjustmentType = (request.adjustment_type ?? "")
                    .Trim().ToUpperInvariant();

                if (!new[] { "ADD", "DEDUCT", "SET" }
                        .Contains(adjustmentType))
                    throw new ArgumentException(
                        "Adjustment type must be ADD, DEDUCT, or SET.");

                if (string.IsNullOrWhiteSpace(request.reason))
                    throw new ArgumentException(
                        "Adjustment reason is required.");
            }

            if (operation != "ADJUSTMENT" && request.quantity <= 0)
                throw new ArgumentException(
                    "Movement quantity must be greater than zero.");

            if (operation == "ADJUSTMENT" &&
                adjustmentType != "SET" && request.quantity <= 0)
                throw new ArgumentException(
                    "Adjustment quantity must be greater than zero.");

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var inventory = await _context.LaboratoryInventories
                .FirstOrDefaultAsync(x =>
                    x.inventory_id == request.inventory_id);

            if (inventory == null)
                throw new KeyNotFoundException("Inventory not found.");

            var item = await _context.LaboratoryItems
                .FirstOrDefaultAsync(x =>
                    x.item_id == inventory.item_id);

            if (item == null || !item.is_active)
                throw new ArgumentException(
                    "Laboratory item is inactive or missing.");

            decimal previousQty = inventory.quantity;
            decimal newQty;
            string transactionType;
            string referenceType;

            if (operation == "IN")
            {
                newQty = previousQty + request.quantity;
                transactionType = "IN";
                referenceType = "STOCK_IN";
            }
            else if (operation == "OUT")
            {
                newQty = previousQty - request.quantity;
                transactionType = "OUT";
                referenceType = "STOCK_OUT";
            }
            else
            {
                if (adjustmentType == "ADD")
                    newQty = previousQty + request.quantity;
                else if (adjustmentType == "DEDUCT")
                    newQty = previousQty - request.quantity;
                else
                    newQty = request.quantity;

                transactionType = newQty >= previousQty
                    ? "IN" : "OUT";

                referenceType = "MANUAL_ADJUSTMENT";
            }

            if (newQty < 0)
                throw new ArgumentException(
                    $"Insufficient stock. Available: {previousQty}.");

            if (item.tracking_type == "SERIAL" &&
                newQty != 0 && newQty != 1)
                throw new ArgumentException(
                    "Serial-tracked inventory quantity must be 0 or 1.");

            decimal movementQty = Math.Abs(newQty - previousQty);

            if (movementQty == 0)
                throw new ArgumentException(
                    "No inventory quantity change detected.");

            inventory.quantity = newQty;
            inventory.updated_at = DateTime.UtcNow;

            var referenceNo =
                string.IsNullOrWhiteSpace(request.reference_no)
                    ? $"LAB-{DateTime.UtcNow:yyyyMMddHHmmssfff}-" +
                      Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
                    : request.reference_no.Trim();

            var transaction = new LaboratoryInventoryTransaction
            {
                inventory_id = inventory.inventory_id,
                item_id = inventory.item_id,
                location_id = inventory.location_id,
                transaction_type = transactionType,
                reference_type = referenceType,
                reference_no = referenceNo,
                quantity = movementQty,
                previous_quantity = previousQty,
                new_quantity = newQty,
                reason = operation == "ADJUSTMENT"
                    ? request.reason?.Trim()
                    : null,
                remarks = request.remarks?.Trim(),
                performed_by = request.performed_by.Trim(),
                created_at = DateTime.UtcNow
            };

            _context.LaboratoryInventoryTransactions.Add(transaction);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return transaction;
        }

        // =====================================================
        // TRANSACTION HISTORY
        // =====================================================

        public async Task<object> GetTransactionsAsync(
            string search = "",
            long? locationId = null,
            string movement = "",
            string referenceType = "",
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            if (fromDate.HasValue && toDate.HasValue &&
                fromDate.Value.Date > toDate.Value.Date)
                throw new ArgumentException("Invalid date range.");

            var query =
                from tx in _context.LaboratoryInventoryTransactions
                    .AsNoTracking()
                join item in _context.LaboratoryItems
                    on tx.item_id equals item.item_id
                join location in _context.LaboratoryLocations
                    on tx.location_id equals location.location_id
                join inv in _context.LaboratoryInventories
                    on tx.inventory_id equals inv.inventory_id
                select new
                {
                    tx.transaction_id,
                    tx.inventory_id,
                    tx.item_id,
                    item.item_code,
                    item.item_description,
                    inv.serial_lot_no,
                    tx.location_id,
                    location_name = location.location_name,
                    tx.transaction_type,
                    tx.reference_type,
                    tx.reference_no,
                    tx.quantity,
                    tx.previous_quantity,
                    tx.new_quantity,
                    item.uom,
                    tx.reason,
                    tx.remarks,
                    tx.performed_by,
                    tx.created_at
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(x =>
                    x.item_code.Contains(keyword) ||
                    x.item_description.Contains(keyword) ||
                    x.reference_no.Contains(keyword) ||
                    x.performed_by.Contains(keyword) ||
                    (x.serial_lot_no != null &&
                     x.serial_lot_no.Contains(keyword)));
            }

            if (locationId.HasValue)
                query = query.Where(x =>
                    x.location_id == locationId.Value);

            if (!string.IsNullOrWhiteSpace(movement))
            {
                var type = movement.Trim().ToUpperInvariant();

                if (type != "IN" && type != "OUT")
                    throw new ArgumentException(
                        "Movement must be IN or OUT.");

                query = query.Where(x => x.transaction_type == type);
            }

            if (!string.IsNullOrWhiteSpace(referenceType))
            {
                var type = referenceType.Trim().ToUpperInvariant();

                query = query.Where(x => x.reference_type == type);
            }

            // Dates are selected in Philippine local time.
            var phZone = GetPhilippineTimeZone();

            if (fromDate.HasValue)
            {
                var fromUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(
                        fromDate.Value.Date,
                        DateTimeKind.Unspecified),
                    phZone);

                query = query.Where(x => x.created_at >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(
                        toDate.Value.Date.AddDays(1),
                        DateTimeKind.Unspecified),
                    phZone);

                query = query.Where(x => x.created_at < toUtc);
            }

            return await query
                .OrderByDescending(x => x.created_at)
                .ThenByDescending(x => x.transaction_id)
                .ToListAsync();
        }

        // =====================================================
        // TRANSACTION DASHBOARD SUMMARY
        // =====================================================

        public async Task<object> GetTransactionSummaryAsync()
        {
            var phZone = GetPhilippineTimeZone();

            var todayPh = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, phZone).Date;

            var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(
                    todayPh, DateTimeKind.Unspecified),
                phZone);

            var tomorrowStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(
                    todayPh.AddDays(1),
                    DateTimeKind.Unspecified),
                phZone);

            var query = _context.LaboratoryInventoryTransactions
                .AsNoTracking();

            return new
            {
                totalTransactions = await query.CountAsync(),

                inTransactions = await query.CountAsync(
                    x => x.transaction_type == "IN"),

                outTransactions = await query.CountAsync(
                    x => x.transaction_type == "OUT"),

                adjustmentTransactions = await query.CountAsync(
                    x => x.reference_type == "MANUAL_ADJUSTMENT"),

                todayTransactions = await query.CountAsync(
                    x => x.created_at >= todayStartUtc &&
                         x.created_at < tomorrowStartUtc)
            };
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static string? NormalizeSerialLot(
            string trackingType, string? serialLot)
        {
            var value = string.IsNullOrWhiteSpace(serialLot)
                ? null
                : serialLot.Trim();

            if (trackingType == "NONE")
                return null;

            if (value == null)
                throw new ArgumentException(
                    "Serial/Lot number is required for this item.");

            return value;
        }

        private static TimeZoneInfo GetPhilippineTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Asia/Manila");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Singapore Standard Time");
            }
        }
    }

    // =========================================================
    // MOVEMENT REQUEST DTO
    // =========================================================

    public class LaboratoryMovementRequest
    {
        public long inventory_id { get; set; }

        // IN / OUT: quantity to move
        // ADJUSTMENT ADD / DEDUCT: quantity to change
        // ADJUSTMENT SET: desired final quantity
        public decimal quantity { get; set; }

        // ADD / DEDUCT / SET
        public string? adjustment_type { get; set; }

        public string? reference_no { get; set; }

        public string? reason { get; set; }

        public string? remarks { get; set; }

        public string performed_by { get; set; } = "";
    }
}
