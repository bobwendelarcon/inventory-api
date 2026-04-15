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

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            var products = await _context.Products
                .OrderBy(x => x.product_name)
                .ToListAsync();

            return products.Select(x => new Dictionary<string, object>
            {
                { "product_id", x.product_id },
                { "product_sku", x.product_sku ?? "" },
                { "product_name", x.product_name },
                { "product_description", x.product_description ?? "" },
                { "product_price", x.product_price },
                { "uom", x.uom ?? "" },
                { "pack_uom", x.pack_uom ?? "" },
                { "pack_qty", x.pack_qty },
                { "stock_level", x.stock_level },
                { "catg_id", x.catg_id ?? "" },
                { "is_deleted", x.is_deleted },
                { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
            }).ToList();
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

        public async Task AddAsync(CreateProductDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.product_id))
                throw new Exception("product_id is required.");

            if (string.IsNullOrWhiteSpace(dto.product_name))
                throw new Exception("product_name is required.");

            bool exists = await _context.Products.AnyAsync(x => x.product_id == dto.product_id);

            if (exists)
                throw new Exception("Product already exists.");

            var product = new Product
            {
                product_id = dto.product_id,
                product_sku = dto.product_sku,
                product_name = dto.product_name,
                product_description = dto.product_description,
                product_price = dto.product_price,
                uom = dto.uom,
                pack_uom = dto.pack_uom,
                pack_qty = dto.pack_qty,
                stock_level = dto.stock_level,
                catg_id = dto.catg_id,
                is_deleted = false,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

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
            product.stock_level = dto.stock_level;
            product.catg_id = dto.catg_id;
            product.is_deleted = dto.is_deleted;
            product.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(string productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.product_id == productId);

            if (product == null)
                return false;

            product.is_deleted = true;
            product.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ResetAllAsync()
        {
            var products = await _context.Products.ToListAsync();

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();
        }
    }
}