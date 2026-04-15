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
                existingLot.updated_at = DateTime.Now;

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
                created_at = DateTime.Now,
                updated_at = DateTime.Now,
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
    }
}