using inventory_api.Data;
using inventory_api.DTOs.Manufacturing.Materials;
using inventory_api.Models.Manufacturing.Materials;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Manufacturing.Materials
{
    public class MaterialSubCategoryService
    {
        private readonly AppDbContext _context;

        public MaterialSubCategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<object>> GetAllAsync()
        {
            return await _context.MaterialSubCategories
                .Include(x => x.Category)
                .Where(x => !x.is_deleted)
                .OrderBy(x => x.subcategory_name)
                .Select(x => new
                {
                    x.material_subcategory_id,
                    x.material_category_id,
                    category_name = x.Category!.category_name,
                    x.subcategory_name,
                    x.description,
                    x.is_active
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<List<object>> GetByCategoryAsync(int categoryId)
        {
            return await _context.MaterialSubCategories
                .Where(x =>
                    x.material_category_id == categoryId &&
                    !x.is_deleted)
                .OrderBy(x => x.subcategory_name)
                .Select(x => new
                {
                    x.material_subcategory_id,
                    x.subcategory_name,
                    x.description,
                    x.is_active
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task AddAsync(CreateMaterialSubCategoryDto dto)
        {
            var category = await _context.MaterialCategories
                .FirstOrDefaultAsync(x =>
                    x.material_category_id == dto.material_category_id &&
                    x.is_active);

            if (category == null)
                throw new Exception("Category not found.");

            var exists = await _context.MaterialSubCategories
                .AnyAsync(x =>
                    x.material_category_id == dto.material_category_id &&
                    x.subcategory_name == dto.subcategory_name &&
                    !x.is_deleted);

            if (exists)
                throw new Exception("Sub category already exists.");

            var entity = new MaterialSubCategory
            {
                material_category_id = dto.material_category_id,
                subcategory_name = dto.subcategory_name.Trim(),
                description = dto.description,
                is_active = true,
                is_deleted = false,
                created_at = DateTime.UtcNow
            };

            _context.MaterialSubCategories.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(
            int id,
            CreateMaterialSubCategoryDto dto)
        {
            var entity = await _context.MaterialSubCategories
                .FirstOrDefaultAsync(x =>
                    x.material_subcategory_id == id &&
                    !x.is_deleted);

            if (entity == null)
                throw new Exception("Sub category not found.");

            entity.material_category_id = dto.material_category_id;
            entity.subcategory_name = dto.subcategory_name.Trim();
            entity.description = dto.description;
            entity.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var entity = await _context.MaterialSubCategories
                .FirstOrDefaultAsync(x =>
                    x.material_subcategory_id == id &&
                    !x.is_deleted);

            if (entity == null)
                return false;

            entity.is_deleted = true;
            entity.is_active = false;
            entity.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}