using inventory_api.Data;
using inventory_api.DTOs.Manufacturing.Materials;
using inventory_api.Models.Manufacturing.Materials;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Manufacturing.Materials
{
    public class MaterialService
    {
        private readonly AppDbContext _context;

        public MaterialService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetAllAsync(
            int page = 1,
            int pageSize = 50,
            string? search = null,
            int? categoryId = null,
            int? subCategoryId = null,
            bool? status = null)
        {
            var query = _context.Materials
                .Include(x => x.Category)
                .Include(x => x.SubCategory)
                .Where(x => !x.is_deleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.material_code.Contains(search) ||
                    x.material_name.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(x =>
                    x.material_category_id == categoryId.Value);
            }

            if (subCategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.material_subcategory_id == subCategoryId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x =>
                    x.is_active == status.Value);
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(x => x.material_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.material_id,
                    x.material_code,
                    x.material_name,

                    x.material_category_id,
                    category_name = x.Category != null
                        ? x.Category.category_name
                        : null,

                    x.material_subcategory_id,
                    subcategory_name = x.SubCategory != null
                        ? x.SubCategory.subcategory_name
                        : null,

                    x.uom,
                    x.pack_uom,
                    x.pack_qty,
                    x.minimum_stock,
                    x.description,
                    x.is_lot_tracked,
                    x.is_active
                })
                .ToListAsync();

            return new
            {
                total,
                page,
                pageSize,
                data
            };
        }

        public async Task<object?> GetByIdAsync(int id)
        {
            return await _context.Materials
                .Include(x => x.Category)
                .Include(x => x.SubCategory)
                .Where(x => x.material_id == id && !x.is_deleted)
                .Select(x => new
                {
                    x.material_id,
                    x.material_code,
                    x.material_name,

                    x.material_category_id,
                    category_name = x.Category != null
                        ? x.Category.category_name
                        : null,

                    x.material_subcategory_id,
                    subcategory_name = x.SubCategory != null
                        ? x.SubCategory.subcategory_name
                        : null,

                    x.uom,
                    x.pack_uom,
                    x.pack_qty,
                    x.minimum_stock,
                    x.description,
                    x.is_lot_tracked,
                    x.is_active,
                    x.created_at,
                    x.updated_at
                })
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(CreateMaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.material_code))
                throw new Exception("Material code is required.");

            if (string.IsNullOrWhiteSpace(dto.material_name))
                throw new Exception("Material name is required.");

            if (!dto.material_category_id.HasValue)
                throw new Exception("Category is required.");

            if (string.IsNullOrWhiteSpace(dto.uom))
                throw new Exception("UOM is required.");

            var exists = await _context.Materials
                .AnyAsync(x =>
                    x.material_code == dto.material_code.Trim() &&
                    !x.is_deleted);

            if (exists)
                throw new Exception("Material code already exists.");

            var categoryExists = await _context.MaterialCategories
                .AnyAsync(x =>
                    x.material_category_id == dto.material_category_id.Value &&
                    x.is_active);

            if (!categoryExists)
                throw new Exception("Category not found.");

            if (dto.material_subcategory_id.HasValue)
            {
                var subCategoryExists = await _context.MaterialSubCategories
                    .AnyAsync(x =>
                        x.material_subcategory_id == dto.material_subcategory_id.Value &&
                        x.material_category_id == dto.material_category_id.Value &&
                        x.is_active &&
                        !x.is_deleted);

                if (!subCategoryExists)
                    throw new Exception("Sub category not found under selected category.");
            }

            var material = new Material
            {
                material_code = dto.material_code.Trim(),
                material_name = dto.material_name.Trim(),
                material_category_id = dto.material_category_id,
                material_subcategory_id = dto.material_subcategory_id,
                uom = dto.uom.Trim().ToUpper(),
                pack_uom = dto.pack_uom?.Trim().ToUpper(),
                pack_qty = dto.pack_qty,
                minimum_stock = dto.minimum_stock,
                description = dto.description,
                is_lot_tracked = dto.is_lot_tracked,
                is_active = true,
                is_deleted = false,
                created_at = DateTime.UtcNow
            };

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, CreateMaterialDto dto)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(x =>
                    x.material_id == id &&
                    !x.is_deleted);

            if (material == null)
                throw new Exception("Material not found.");

            if (string.IsNullOrWhiteSpace(dto.material_code))
                throw new Exception("Material code is required.");

            if (string.IsNullOrWhiteSpace(dto.material_name))
                throw new Exception("Material name is required.");

            if (!dto.material_category_id.HasValue)
                throw new Exception("Category is required.");

            if (string.IsNullOrWhiteSpace(dto.uom))
                throw new Exception("UOM is required.");

            var codeExists = await _context.Materials
                .AnyAsync(x =>
                    x.material_id != id &&
                    x.material_code == dto.material_code.Trim() &&
                    !x.is_deleted);

            if (codeExists)
                throw new Exception("Material code already exists.");

            var categoryExists = await _context.MaterialCategories
                .AnyAsync(x =>
                    x.material_category_id == dto.material_category_id.Value &&
                    x.is_active);

            if (!categoryExists)
                throw new Exception("Category not found.");

            if (dto.material_subcategory_id.HasValue)
            {
                var subCategoryExists = await _context.MaterialSubCategories
                    .AnyAsync(x =>
                        x.material_subcategory_id == dto.material_subcategory_id.Value &&
                        x.material_category_id == dto.material_category_id.Value &&
                        x.is_active &&
                        !x.is_deleted);

                if (!subCategoryExists)
                    throw new Exception("Sub category not found under selected category.");
            }

            material.material_code = dto.material_code.Trim();
            material.material_name = dto.material_name.Trim();
            material.material_category_id = dto.material_category_id;
            material.material_subcategory_id = dto.material_subcategory_id;
            material.uom = dto.uom.Trim().ToUpper();
            material.pack_uom = dto.pack_uom?.Trim().ToUpper();
            material.pack_qty = dto.pack_qty;
            material.minimum_stock = dto.minimum_stock;
            material.description = dto.description;
            material.is_lot_tracked = dto.is_lot_tracked;
            material.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(x =>
                    x.material_id == id &&
                    !x.is_deleted);

            if (material == null)
                return false;

            material.is_deleted = true;
            material.is_active = false;
            material.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<object>> GetLookupAsync(
            int? categoryId = null,
            int? subCategoryId = null,
            string? search = null)
        {
            var query = _context.Materials
                .Include(x => x.Category)
                .Include(x => x.SubCategory)
                .Where(x => !x.is_deleted && x.is_active)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(x =>
                    x.material_category_id == categoryId.Value);
            }

            if (subCategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.material_subcategory_id == subCategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.material_code.Contains(search) ||
                    x.material_name.Contains(search));
            }

            var data = await query
                .OrderBy(x => x.material_name)
                .Take(50)
                .Select(x => new
                {
                    x.material_id,
                    x.material_code,
                    x.material_name,

                    x.material_category_id,
                    category_name = x.Category != null
                        ? x.Category.category_name
                        : null,

                    x.material_subcategory_id,
                    subcategory_name = x.SubCategory != null
                        ? x.SubCategory.subcategory_name
                        : null,

                    x.uom,
                    x.is_lot_tracked
                })
                .ToListAsync();

            return data.Cast<object>().ToList();
        }
    }
}