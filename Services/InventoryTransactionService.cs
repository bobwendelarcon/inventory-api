using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class InventoryTransactionService
    {
        private readonly AppDbContext _context;

        public InventoryTransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CreateInventoryTransactionDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.product_id))
                throw new Exception("product_id is required.");

            if (string.IsNullOrWhiteSpace(dto.branch_id))
                throw new Exception("branch_id is required.");

            if (string.IsNullOrWhiteSpace(dto.lot_no))
                throw new Exception("lot_no is required.");

            if (string.IsNullOrWhiteSpace(dto.transaction_type))
                throw new Exception("transaction_type is required.");

            if (dto.quantity <= 0)
                throw new Exception("quantity must be greater than 0.");

            string transactionType = dto.transaction_type.Trim().ToUpper();

            if (transactionType != "IN" && transactionType != "OUT")
                throw new Exception("transaction_type must be IN or OUT.");

            // ✅ BUSINESS RULE:
            // same lot + same product = allowed
            // same lot + different product = reject
            var existingLotForDifferentProduct = await _context.ProductLotNumbers
                .FirstOrDefaultAsync(x =>
                    x.lot_no == dto.lot_no &&
                    x.product_id != dto.product_id &&
                    !x.is_deleted);

            if (existingLotForDifferentProduct != null)
            {
                throw new Exception($"Lot number '{dto.lot_no}' is already assigned to another product.");
            }

            var hasOuterTransaction = _context.Database.CurrentTransaction != null;
            var transaction = hasOuterTransaction
                ? null
                : await _context.Database.BeginTransactionAsync();

            try
            {
                var lot = await _context.ProductLotNumbers
                    .FirstOrDefaultAsync(x =>
                        x.product_id == dto.product_id &&
                        x.branch_id == dto.branch_id &&
                        x.lot_no == dto.lot_no);

                decimal existingQty = lot?.quantity ?? 0;

                string? supplierId = null;
                string? customerId = null;

                if (transactionType == "IN")
                {
                    decimal newQty = existingQty + (decimal)dto.quantity;

                    supplierId = string.IsNullOrWhiteSpace(dto.supplier_id) ? null : dto.supplier_id;
                    customerId = null;

                    if (lot == null)
                    {
                        lot = new ProductLotNumber
                        {
                            product_id = dto.product_id,
                            branch_id = dto.branch_id,
                            lot_no = dto.lot_no,
                            quantity = newQty,
                            manufacturing_date = dto.manufacturing_date,
                            expiration_date = dto.expiration_date,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now,
                            is_deleted = false
                        };

                        _context.ProductLotNumbers.Add(lot);
                    }
                    else
                    {
                        lot.quantity = newQty;
                        lot.updated_at = DateTime.Now;
                    }
                }
                else if (transactionType == "OUT")
                {
                    var lastInTransaction = await _context.InventoryTransactions
                        .Where(x =>
                            x.product_id == dto.product_id &&
                            x.branch_id == dto.branch_id &&
                            x.lot_no == dto.lot_no &&
                            x.transaction_type == "IN" &&
                            !x.is_deleted)
                        .OrderByDescending(x => x.created_at)
                        .FirstOrDefaultAsync();

                    supplierId = lastInTransaction?.supplier_id;
                    customerId = string.IsNullOrWhiteSpace(dto.customer_id) ? null : dto.customer_id;

                    if (lot == null)
                        throw new Exception("Lot not found.");

                    if (existingQty < (decimal)dto.quantity)
                        throw new Exception("Insufficient stock.");

                    lot.quantity = existingQty - (decimal)dto.quantity;
                    lot.updated_at = DateTime.Now;
                }

                var transactionData = new InventoryTransaction
                {
                    product_id = dto.product_id,
                    branch_id = dto.branch_id,
                    transaction_type = transactionType,
                    lot_no = dto.lot_no,
                    quantity = (decimal)dto.quantity,
                    scanned_by = dto.scanned_by ?? "",
                    remarks = dto.remarks ?? "",
                    supplier_id = supplierId,
                    customer_id = customerId,
                    dr_no = dto.dr_no ?? "",
                    inv_no = dto.inv_no ?? "",
                    po_no = dto.po_no ?? "",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                    is_deleted = false
                };

                _context.InventoryTransactions.Add(transactionData);

                await _context.SaveChangesAsync();

                if (transaction != null)
                    await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
            }
        }

        public async Task UpdateReferenceAsync(UpdateTransactionReferenceDto dto)
        {
            if (dto.transaction_id <= 0)
                throw new Exception("transaction_id is required.");

            var transaction = await _context.InventoryTransactions
                .FirstOrDefaultAsync(x => x.transaction_id == dto.transaction_id);

            if (transaction == null)
                throw new Exception("Transaction not found.");

            if ((transaction.transaction_type ?? "").ToUpper() != "OUT")
                throw new Exception("Only OUT transactions can be edited.");

            transaction.dr_no = dto.dr_no ?? "";
            transaction.inv_no = dto.inv_no ?? "";
            transaction.po_no = dto.po_no ?? "";
            transaction.remarks = dto.remarks ?? "";

            if (!string.IsNullOrWhiteSpace(dto.customer))
            {
                var partner = await _context.Partners
                    .FirstOrDefaultAsync(x => x.partner_name == dto.customer);

                transaction.customer_id = partner?.partner_id;
            }
            else
            {
                transaction.customer_id = null;
            }

            transaction.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<string, object>> GetAllAsync(
            int page = 1,
            int pageSize = 30,
            string lot_no = "",
            string product = "",
            string type = "",
            string from = "",
            string to = "",
            string scanned_by = "",
            string full_name = "",
            string reference = "",
            string warehouse = "",
            string order = "desc"
           
        )
        {
            var query = _context.InventoryTransactions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(lot_no))
                query = query.Where(x => x.lot_no.Contains(lot_no));

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(x => x.transaction_type == type);

            if (!string.IsNullOrWhiteSpace(scanned_by))
                query = query.Where(x => x.scanned_by.Contains(scanned_by));

            if (!string.IsNullOrWhiteSpace(warehouse))
                query = query.Where(x => x.branch_id == warehouse);

            if (!string.IsNullOrWhiteSpace(reference))
            {
                query = query.Where(x =>
                    (x.dr_no ?? "").Contains(reference) ||
                    (x.inv_no ?? "").Contains(reference) ||
                    (x.po_no ?? "").Contains(reference) ||
                     (x.remarks ?? "").Contains(reference)
                    );
            }

            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
                query = query.Where(x => x.created_at >= fromDate);

            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
            {
                toDate = toDate.AddDays(1);
                query = query.Where(x => x.created_at < toDate);
            }

            if (!string.IsNullOrWhiteSpace(product))
            {
                var productIds = await _context.Products
                    .Where(p => p.product_name.Contains(product))
                    .Select(p => p.product_id)
                    .ToListAsync();

                query = query.Where(x => productIds.Contains(x.product_id));
            }

            if (!string.IsNullOrWhiteSpace(full_name))
            {
                var userIds = await _context.Users
                    .Where(u => u.full_name.Contains(full_name))
                    .Select(u => u.user_id)
                    .ToListAsync();

                query = query.Where(x => userIds.Contains(x.scanned_by));
            }

            query = order?.ToLower() == "asc"
                ? query.OrderBy(x => x.created_at)
                : query.OrderByDescending(x => x.created_at);

            var total = await query.CountAsync();

            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var products = await _context.Products.ToListAsync();
            var productDict = products.ToDictionary(x => x.product_id, x => x);

            var partners = await _context.Partners.ToListAsync();
            var partnerDict = partners.ToDictionary(x => x.partner_id, x => x.partner_name);

            var users = await _context.Users.ToListAsync();
            var userDict = users.ToDictionary(x => x.user_id, x => x.full_name);

            var branches = await _context.Branches.ToListAsync();
            var branchDict = branches.ToDictionary(x => x.branch_id, x => x.branch_name);

            var result = new List<Dictionary<string, object>>();

            foreach (var t in transactions)
            {
                productDict.TryGetValue(t.product_id, out var productData);

                string supplierName = "";
                string customerName = "";
                string fullNameValue = "";

                if (!string.IsNullOrEmpty(t.supplier_id) && partnerDict.ContainsKey(t.supplier_id))
                {
                    supplierName = partnerDict[t.supplier_id];
                }

                if (!string.IsNullOrEmpty(t.customer_id) && partnerDict.ContainsKey(t.customer_id))
                {
                    customerName = partnerDict[t.customer_id];
                }

                if (!string.IsNullOrEmpty(t.scanned_by) && userDict.ContainsKey(t.scanned_by))
                {
                    fullNameValue = userDict[t.scanned_by];
                }

                string branchName = "";

                if (!string.IsNullOrEmpty(t.branch_id) && branchDict.ContainsKey(t.branch_id))
                {
                    branchName = branchDict[t.branch_id];
                }

                result.Add(new Dictionary<string, object>
                {
                    { "transaction_id", t.transaction_id },
                    { "product_id", t.product_id },
                    { "product_name", productData?.product_name ?? "" },
                    { "product_description", productData?.product_description ?? "" },
                    { "branch_id", t.branch_id },
                    { "branch_name", branchName },
                    { "transaction_type", t.transaction_type },
                    { "lot_no", t.lot_no },
                    { "quantity", t.quantity },
                    { "scanned_by", t.scanned_by },
                    { "full_name", fullNameValue },
                    { "remarks", t.remarks },
                    { "supplier_id", t.supplier_id },
                    { "customer_id", t.customer_id },
                    { "supplier_name", supplierName },
                    { "customer_name", customerName },
                    { "dr_no", t.dr_no },
                    { "inv_no", t.inv_no },
                    { "po_no", t.po_no },
                    { "created_at", t.created_at.ToString("yyyy-MM-dd HH:mm:ss") }
                });
            }

            return new Dictionary<string, object>
            {
                { "data", result },
                { "total", total },
                { "page", page },
                { "pageSize", pageSize }
            };
        }

        //public async Task TransferAsync(TransferDto dto)
        //{
        //    if (dto == null)
        //        throw new Exception("Invalid request.");

        //    if (string.IsNullOrWhiteSpace(dto.product_id))
        //        throw new Exception("product_id is required.");

        //    if (string.IsNullOrWhiteSpace(dto.lot_no))
        //        throw new Exception("lot_no is required.");

        //    if (string.IsNullOrWhiteSpace(dto.from_branch))
        //        throw new Exception("from_branch is required.");

        //    if (string.IsNullOrWhiteSpace(dto.to_branch))
        //        throw new Exception("to_branch is required.");

        //    if (dto.from_branch == dto.to_branch)
        //        throw new Exception("Source and destination cannot be the same.");

        //    if (dto.quantity <= 0)
        //        throw new Exception("quantity must be greater than 0.");

        //    using var trx = await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        await AddAsync(new CreateInventoryTransactionDto
        //        {
        //            product_id = dto.product_id,
        //            branch_id = dto.from_branch,
        //            lot_no = dto.lot_no,
        //            quantity = (double)dto.quantity,
        //            transaction_type = "OUT",
        //            scanned_by = dto.scanned_by ?? "",
        //            remarks = string.IsNullOrWhiteSpace(dto.remarks)
        //                ? $"Transfer OUT to {dto.to_branch}"
        //                : dto.remarks
        //        });

        //        await AddAsync(new CreateInventoryTransactionDto
        //        {
        //            product_id = dto.product_id,
        //            branch_id = dto.to_branch,
        //            lot_no = dto.lot_no,
        //            quantity = (double)dto.quantity,
        //            transaction_type = "IN",
        //            scanned_by = dto.scanned_by ?? "",
        //            remarks = string.IsNullOrWhiteSpace(dto.remarks)
        //                ? $"Transfer IN from {dto.from_branch}"
        //                : dto.remarks
        //        });

        //        await trx.CommitAsync();
        //    }
        //    catch
        //    {
        //        await trx.RollbackAsync();
        //        throw;
        //    }
        //}

        public async Task TransferAsync(TransferDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.product_id))
                throw new Exception("product_id is required.");

            if (string.IsNullOrWhiteSpace(dto.lot_no))
                throw new Exception("lot_no is required.");

            if (string.IsNullOrWhiteSpace(dto.from_branch))
                throw new Exception("from_branch is required.");

            if (string.IsNullOrWhiteSpace(dto.to_branch))
                throw new Exception("to_branch is required.");

            if (dto.from_branch == dto.to_branch)
                throw new Exception("Source and destination cannot be the same.");

            if (dto.quantity <= 0)
                throw new Exception("quantity must be greater than 0.");

            var fromBranch = await _context.Branches
                .FirstOrDefaultAsync(x => x.branch_id == dto.from_branch);

            var toBranch = await _context.Branches
                .FirstOrDefaultAsync(x => x.branch_id == dto.to_branch);

            string fromBranchName = fromBranch?.branch_name ?? dto.from_branch;
            string toBranchName = toBranch?.branch_name ?? dto.to_branch;

            var sourceLot = await _context.ProductLotNumbers
                .FirstOrDefaultAsync(x =>
                    x.product_id == dto.product_id &&
                    x.branch_id == dto.from_branch &&
                    x.lot_no == dto.lot_no &&
                    !x.is_deleted);

            if (sourceLot == null)
                throw new Exception("Source lot not found.");

            var sourceSupplierTransaction = await _context.InventoryTransactions
                .Where(x =>
                    x.product_id == dto.product_id &&
                    x.branch_id == dto.from_branch &&
                    x.lot_no == dto.lot_no &&
                    x.transaction_type == "IN" &&
                    !x.is_deleted)
                .OrderByDescending(x => x.created_at)
                .FirstOrDefaultAsync();

            string? supplierId = sourceSupplierTransaction?.supplier_id;

            using var trx = await _context.Database.BeginTransactionAsync();

            try
            {
                await AddAsync(new CreateInventoryTransactionDto
                {
                    product_id = dto.product_id,
                    branch_id = dto.from_branch,
                    lot_no = dto.lot_no,
                    quantity = (double)dto.quantity,
                    transaction_type = "OUT",
                    scanned_by = dto.scanned_by ?? "",
                    remarks = string.IsNullOrWhiteSpace(dto.remarks)
                        ? $"Transfer to {toBranchName}"
                        : dto.remarks
                });

                await AddAsync(new CreateInventoryTransactionDto
                {
                    product_id = dto.product_id,
                    branch_id = dto.to_branch,
                    lot_no = dto.lot_no,
                    quantity = (double)dto.quantity,
                    transaction_type = "IN",
                    scanned_by = dto.scanned_by ?? "",
                    remarks = string.IsNullOrWhiteSpace(dto.remarks)
                        ? $"Transfer from {fromBranchName}"
                        : dto.remarks,
                    manufacturing_date = sourceLot.manufacturing_date,
                    expiration_date = sourceLot.expiration_date,
                    supplier_id = supplierId
                });

                await trx.CommitAsync();
            }
            catch
            {
                await trx.RollbackAsync();
                throw;
            }
        }

        public async Task ClearAllDataAsync()
        {
            _context.InventoryTransactions.RemoveRange(_context.InventoryTransactions);
            _context.ProductLotNumbers.RemoveRange(_context.ProductLotNumbers);

            await _context.SaveChangesAsync();
        }
    }
}