using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetAllAsync(
           int page = 1,
           int pageSize = 50,
           string? search = null,
           string? categoryId = null,
           bool? status = null,
           string? source = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();

                query = query.Where(x =>
                    x.product_id.ToLower().Contains(keyword) ||
                    x.product_name.ToLower().Contains(keyword) ||
                    (x.product_sku ?? "").ToLower().Contains(keyword) ||
                    (x.product_description ?? "").ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(categoryId))
                query = query.Where(x => x.catg_id == categoryId);

            if (status.HasValue)
                query = query.Where(x => x.is_deleted == status.Value);

            if (!string.IsNullOrWhiteSpace(source))
                query = query.Where(x => x.product_source == source);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.product_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new Dictionary<string, object>
                {
            { "product_id", x.product_id },
            { "product_sku", x.product_sku ?? "" },
            { "product_name", x.product_name },
            { "product_description", x.product_description ?? "" },
            { "product_price", x.product_price },
            { "uom", x.uom ?? "" },
            { "pack_uom", x.pack_uom ?? "" },
            { "pack_qty", x.pack_qty ?? 0 },
            { "stock_level", x.stock_level },
            { "catg_id", x.catg_id ?? "" },
            { "is_deleted", x.is_deleted },
            { "product_source", x.product_source ?? "OWN" },
            { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
            { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
                })
                .ToListAsync();

            return new
            {
                items,
                page,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            };
        }
        public async Task<Dictionary<string, object>?> GetByBarcodeAsync(string product_sku)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.product_sku == product_sku && !x.is_deleted);

            if (product == null)
                return null;

            return new Dictionary<string, object>
            {
                { "product_id", product.product_id },
                { "product_sku", product.product_sku ?? "" },
                { "product_name", product.product_name },
                { "product_description", product.product_description ?? "" },
                { "product_price", product.product_price },
                { "uom", product.uom ?? "" },
                { "pack_uom", product.pack_uom ?? "" },
                { "pack_qty", product.pack_qty },
                { "stock_level", product.stock_level },
                { "catg_id", product.catg_id ?? "" },
                { "is_deleted", product.is_deleted },

                { "created_at", product.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", product.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }

        //auto generated
        public async Task AddAsync(CreateProductDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.product_name))
                throw new Exception("product_name is required.");

            string newProductId = await GenerateProductIdAsync();

            bool exists = await _context.Products.AnyAsync(x => x.product_id == newProductId);

            if (exists)
                throw new Exception("Generated Product ID already exists. Please try again.");

            var product = new Product
            {
                product_id = newProductId,
                product_sku = dto.product_sku,
                product_name = dto.product_name.Trim(),
                product_description = dto.product_description,
                product_price = dto.product_price,
                uom = dto.uom,
                product_source = string.IsNullOrWhiteSpace(dto.product_source)
                    ? "OWN"
                    : dto.product_source,
                pack_uom = dto.pack_uom,
                pack_qty = dto.pack_qty,
                stock_level = dto.stock_level,
                catg_id = dto.catg_id,
                is_deleted = false,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        private async Task<string> GenerateProductIdAsync()
        {
            var lastProduct = await _context.Products
                .Where(x => x.product_id.StartsWith("prod"))
                .OrderByDescending(x => x.product_id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastProduct != null)
            {
                string numberPart = lastProduct.product_id.Replace("prod", "");

                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"prod{nextNumber:D4}";
        }



        //    public async Task AddAsync(CreateProductDto dto)
        //    {
        //        if (dto == null)
        //            throw new Exception("Invalid request.");

        //        if (string.IsNullOrWhiteSpace(dto.product_id))
        //            throw new Exception("product_id is required.");

        //        if (string.IsNullOrWhiteSpace(dto.product_name))
        //            throw new Exception("product_name is required.");

        //        bool exists = await _context.Products.AnyAsync(x => x.product_id == dto.product_id);

        //        if (exists)
        //            throw new Exception("Product already exists.");

        //        var product = new Product
        //        {
        //            product_id = dto.product_id,
        //            product_sku = dto.product_sku,
        //            product_name = dto.product_name,
        //            product_description = dto.product_description,
        //            product_price = dto.product_price,
        //            uom = dto.uom,
        //            product_source = string.IsNullOrWhiteSpace(dto.product_source)
        //? "OWN"
        //: dto.product_source,
        //            pack_uom = dto.pack_uom,
        //            pack_qty = dto.pack_qty,
        //            stock_level = dto.stock_level,
        //            catg_id = dto.catg_id,
        //            is_deleted = false,
        //            created_at = DateTime.UtcNow,
        //            updated_at = DateTime.UtcNow
        //        };

        //        _context.Products.Add(product);
        //        await _context.SaveChangesAsync();
        //    }

        public async Task UpdateAsync(string id, CreateProductDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(id))
                throw new Exception("Product id is required.");

            if (string.IsNullOrWhiteSpace(dto.product_name))
                throw new Exception("product_name is required.");

            var product = await _context.Products.FirstOrDefaultAsync(x => x.product_id == id);

            if (product == null)
                throw new Exception("Product not found.");

            product.product_sku = dto.product_sku;
            product.product_name = dto.product_name;
            product.product_description = dto.product_description;
            product.product_price = dto.product_price;
            product.uom = dto.uom;
            product.pack_uom = dto.pack_uom;
            product.pack_qty = dto.pack_qty;
            product.product_source = string.IsNullOrWhiteSpace(dto.product_source)
    ? product.product_source ?? "OWN"
    : dto.product_source;
            product.stock_level = dto.stock_level;
            product.catg_id = dto.catg_id;
            product.is_deleted = dto.is_deleted;
            product.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(string productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.product_id == productId);

            if (product == null)
                return false;

            product.is_deleted = true;
            product.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ResetAllAsync()
        {
            var products = await _context.Products.ToListAsync();

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductLookupDto>> GetProductsLookupAsync(string? categoryId, string? search)
        {
            var query = _context.Products
                .Where(x => !x.is_deleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoryId))
                query = query.Where(x => x.catg_id == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(x => x.product_name.ToLower().Contains(keyword));
            }

            return await query
                .OrderBy(x => x.product_name)
                .Select(x => new ProductLookupDto
                {
                    ProductId = x.product_id,
                    ProductName = x.product_name,
                    CategoryId = x.catg_id,
                    Uom = x.uom,
                    PackUom = x.pack_uom,
                    PackQty = x.pack_qty
                })
                .ToListAsync();
        }
    }
}