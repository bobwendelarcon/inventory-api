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

            string lotDocId = $"{dto.product_id}_{dto.branch_id}_{dto.lot_no}";

            using var transaction = await _context.Database.BeginTransactionAsync();

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
                            //doc_id = lotDocId,
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
                //else if (transactionType == "OUT")
                //{
                //    supplierId = null;
                //    customerId = string.IsNullOrWhiteSpace(dto.customer_id) ? null : dto.customer_id;

                //    if (lot == null)
                //        throw new Exception("Lot not found.");

                //    if (existingQty < (decimal)dto.quantity)
                //        throw new Exception("Insufficient stock.");

                //    lot.quantity = existingQty - (decimal)dto.quantity;
                //    lot.updated_at = DateTime.Now;
                //}

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
                    //transaction_id = Guid.NewGuid().ToString(),
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
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //    public async Task<List<Dictionary<string, object>>> GetAllAsync()
        //    {
        //        var transactions = await _context.InventoryTransactions
        //            .OrderByDescending(x => x.created_at)
        //            .ToListAsync();

        //        var products = await _context.Products.ToListAsync();

        //        var productDict = products.ToDictionary(x => x.product_id, x => x);

        //        var partners = await _context.Partners.ToListAsync();
        //        var partnerDict = partners.ToDictionary(x => x.partner_id, x => x.partner_name);

        //        var result = new List<Dictionary<string, object>>();

        //        foreach (var t in transactions)
        //        {
        //            productDict.TryGetValue(t.product_id, out var product);

        //            string supplierName = "";
        //            string customerName = "";

        //            if (!string.IsNullOrEmpty(t.supplier_id) && partnerDict.ContainsKey(t.supplier_id))
        //            {
        //                supplierName = partnerDict[t.supplier_id];
        //            }

        //            if (!string.IsNullOrEmpty(t.customer_id) && partnerDict.ContainsKey(t.customer_id))
        //            {
        //                customerName = partnerDict[t.customer_id];
        //            }

        //            result.Add(new Dictionary<string, object>
        //{
        //    { "transaction_id", t.transaction_id },
        //    { "product_id", t.product_id },
        //    { "product_name", product?.product_name ?? "" },
        //    { "product_description", product?.product_description ?? "" },
        //    { "branch_id", t.branch_id },
        //    { "transaction_type", t.transaction_type },
        //    { "lot_no", t.lot_no },
        //    { "quantity", t.quantity },
        //    { "scanned_by", t.scanned_by },
        //    { "remarks", t.remarks },

        //    { "supplier_id", t.supplier_id },
        //    { "customer_id", t.customer_id },

        //    { "supplier_name", supplierName },   // 🔥 NEW
        //    { "customer_name", customerName },   // 🔥 NEW

        //    { "dr_no", t.dr_no },
        //    { "inv_no", t.inv_no },
        //    { "po_no", t.po_no },
        //    { "created_at", t.created_at.ToString("yyyy-MM-dd HH:mm:ss") }
        //});
        //        }

        //        return result;
        //    }

        public async Task<Dictionary<string, object>> GetAllAsync(
    int page = 1,
    int pageSize = 30,
    string lot_no = "",
    string product = "",
    string type = "",
    string from = "",
    string to = "",
    string scanned_by = "",
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
                    (x.po_no ?? "").Contains(reference));
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

            var result = new List<Dictionary<string, object>>();

            foreach (var t in transactions)
            {
                productDict.TryGetValue(t.product_id, out var productData);

                string supplierName = "";
                string customerName = "";

                if (!string.IsNullOrEmpty(t.supplier_id) && partnerDict.ContainsKey(t.supplier_id))
                {
                    supplierName = partnerDict[t.supplier_id];
                }

                if (!string.IsNullOrEmpty(t.customer_id) && partnerDict.ContainsKey(t.customer_id))
                {
                    customerName = partnerDict[t.customer_id];
                }

                result.Add(new Dictionary<string, object>
        {
            { "transaction_id", t.transaction_id },
            { "product_id", t.product_id },
            { "product_name", productData?.product_name ?? "" },
            { "product_description", productData?.product_description ?? "" },
            { "branch_id", t.branch_id },
            { "transaction_type", t.transaction_type },
            { "lot_no", t.lot_no },
            { "quantity", t.quantity },
            { "scanned_by", t.scanned_by },
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

        public async Task ClearAllDataAsync()
        {
            _context.InventoryTransactions.RemoveRange(_context.InventoryTransactions);
            _context.ProductLotNumbers.RemoveRange(_context.ProductLotNumbers);

            await _context.SaveChangesAsync();
        }
    }
}