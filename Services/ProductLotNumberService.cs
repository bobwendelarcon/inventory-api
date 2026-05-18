using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ProductLotNumberService
    {
        private readonly AppDbContext _context;

        public ProductLotNumberService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            var lots = await _context.ProductLotNumbers.ToListAsync();
            var products = await _context.Products.ToListAsync();

            var productDict = products.ToDictionary(x => x.product_id, x => x);

            return lots.Select(x =>
            {
                productDict.TryGetValue(x.product_id, out var product);

                return new Dictionary<string, object>
        {
            { "product_id", x.product_id },
            { "branch_id", x.branch_id },
            { "lot_no", x.lot_no },
            { "qty", x.quantity }, // 🔥 rename to match frontend

            { "pack_qty", product?.pack_qty ?? 0 },
            { "pack_uom", product?.pack_uom ?? "" },
            { "uom", product?.uom ?? "" }, // 🔥 IMPORTANT

            { "manufacturing_date", x.manufacturing_date?.ToString("yyyy-MM-dd") ?? "" },
            { "expiration_date", x.expiration_date?.ToString("yyyy-MM-dd") ?? "" },
            { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
            { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
        };
            }).ToList();
        }

        //public async Task<List<Dictionary<string, object>>> GetAllAsync()
        //{
        //    var lots = await _context.ProductLotNumbers.ToListAsync();

        //    return lots.Select(x => new Dictionary<string, object>
        //    {
        //        { "product_id", x.product_id },
        //        { "branch_id", x.branch_id },
        //        { "lot_no", x.lot_no },
        //        { "quantity", x.quantity },
        //        { "pack_qty", product?.pack_qty ?? 0 },
        //         { "pack_uom", product?.pack_uom ?? "" },
        //        { "manufacturing_date", x.manufacturing_date?.ToString("yyyy-MM-dd") ?? "" },
        //        { "expiration_date", x.expiration_date?.ToString("yyyy-MM-dd") ?? "" },
        //        { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
        //        { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
        //    }).ToList();
        //}

        public async Task AddAsync(CreateProductLotNumberDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.product_id))
                throw new Exception("product_id is required.");

            if (string.IsNullOrWhiteSpace(dto.branch_id))
                throw new Exception("branch_id is required.");

            if (string.IsNullOrWhiteSpace(dto.lot_no))
                throw new Exception("lot_no is required.");

            // 🔥 CHECK EXISTING LOT
            var existingLot = await _context.ProductLotNumbers
                .FirstOrDefaultAsync(x =>
                    x.product_id == dto.product_id &&
                    x.branch_id == dto.branch_id &&
                    x.lot_no == dto.lot_no);

            if (existingLot != null)
            {
                // ✅ UPDATE QUANTITY (ADD)
                existingLot.quantity += dto.quantity;
                existingLot.updated_at = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return;
            }

            // ✅ CREATE NEW LOT
            var lot = new ProductLotNumber
            {
                product_id = dto.product_id,
                branch_id = dto.branch_id,
                lot_no = dto.lot_no,
                quantity = dto.quantity,
                manufacturing_date = dto.manufacturing_date,
                expiration_date = dto.expiration_date,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                is_deleted = false
            };

            _context.ProductLotNumbers.Add(lot);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Dictionary<string, object>>> GetByLotNoAsync(string lot_no)
        {
            var lots = await _context.ProductLotNumbers
                .Where(x => x.lot_no == lot_no && !x.is_deleted)
                .OrderBy(x => x.branch_id)
                .ToListAsync();

            var result = new List<Dictionary<string, object>>();

            foreach (var lot in lots)   
            {
                result.Add(new Dictionary<string, object>
        {
            { "product_id", lot.product_id },
            { "branch_id", lot.branch_id },
            { "lot_no", lot.lot_no },
            { "quantity", lot.quantity },
            { "manufacturing_date", lot.manufacturing_date?.ToString("yyyy-MM-dd") ?? "" },
            { "expiration_date", lot.expiration_date?.ToString("yyyy-MM-dd") ?? "" },
            { "created_at", lot.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
            { "updated_at", lot.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
        });
            }

            return result;
        }

        public async Task<List<Dictionary<string, object>>> GetByProductIDAsync(string product_id)
        {
            var lots = await _context.ProductLotNumbers
                .Where(x => x.product_id == product_id)
                .ToListAsync();

            return lots.Select(x => new Dictionary<string, object>
            {
                { "product_id", x.product_id },
                { "branch_id", x.branch_id },
                { "lot_no", x.lot_no },
                { "quantity", x.quantity },
                { "manufacturing_date", x.manufacturing_date?.ToString("yyyy-MM-dd") ?? "" },
                { "expiration_date", x.expiration_date?.ToString("yyyy-MM-dd") ?? "" },
                { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
            }).ToList();
        }

        public async Task ResetAllAsync()
        {
            var lots = await _context.ProductLotNumbers.ToListAsync();

            _context.ProductLotNumbers.RemoveRange(lots);
            await _context.SaveChangesAsync();
        }

        public async Task RenameLotAsync(RenameLotNumberDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (dto.requested_by_role?.Trim().ToUpper() != "ADMIN")
                throw new Exception("Only ADMIN account can edit lot number.");

            if (string.IsNullOrWhiteSpace(dto.product_id))
                throw new Exception("Product is required.");

            if (string.IsNullOrWhiteSpace(dto.branch_id))
                throw new Exception("Branch is required.");

            if (string.IsNullOrWhiteSpace(dto.old_lot_no))
                throw new Exception("Old lot number is required.");

            if (string.IsNullOrWhiteSpace(dto.new_lot_no))
                throw new Exception("New lot number is required.");

            var oldLot = dto.old_lot_no.Trim();
            var newLot = dto.new_lot_no.Trim();

            if (oldLot.Equals(newLot, StringComparison.OrdinalIgnoreCase))
                throw new Exception("New lot number is the same as current lot number.");

            var lot = await _context.ProductLotNumbers.FirstOrDefaultAsync(x =>
                !x.is_deleted &&
                x.product_id == dto.product_id &&
                x.branch_id == dto.branch_id &&
                x.lot_no == oldLot);

            if (lot == null)
                throw new Exception("Lot number not found.");

            var newLotExists = await _context.ProductLotNumbers.AnyAsync(x =>
                !x.is_deleted &&
                x.product_id == dto.product_id &&
                x.branch_id == dto.branch_id &&
                x.lot_no == newLot);

            if (newLotExists)
                throw new Exception("New lot number already exists for this product and branch.");

            lot.lot_no = newLot;
            lot.updated_at = DateTime.UtcNow;

            var transactions = await _context.InventoryTransactions
                .Where(x =>
                    !x.is_deleted &&
                    x.product_id == dto.product_id &&
                    x.branch_id == dto.branch_id &&
                    x.lot_no == oldLot)
                .ToListAsync();

            foreach (var trx in transactions)
            {
                trx.lot_no = newLot;
                trx.updated_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<object> CheckSimilarLotAsync(
    string productId,
    string branchId,
    string lotNo)
        {
            if (string.IsNullOrWhiteSpace(lotNo))
            {
                return new
                {
                    hasSimilar = false,
                    similarLots = new List<string>()
                };
            }

            lotNo = lotNo.Trim().ToUpper();

            var existingLots = await _context.ProductLotNumbers
                .Where(x =>
                    !x.is_deleted &&
                    x.product_id == productId &&
                    x.branch_id == branchId)
                .Select(x => x.lot_no)
                .Distinct()
                .ToListAsync();

            var similarLots = existingLots
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x) &&
                    x.ToUpper() != lotNo &&
                    LevenshteinDistance(x.ToUpper(), lotNo) <= 2)
                .Take(5)
                .ToList();

            return new
            {
                hasSimilar = similarLots.Any(),
                similarLots
            };
        }

        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;

            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(
                            d[i - 1, j] + 1,
                            d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}