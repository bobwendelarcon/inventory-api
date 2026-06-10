using inventory_api.Data;
using inventory_api.DTOs.Manufacturing.Materials;
using inventory_api.Models.Manufacturing.Materials;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Manufacturing.Materials
{
    public class MaterialCategoryService
    {
        private readonly AppDbContext _context;

        public MaterialCategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MaterialCategory>> GetAllAsync()
        {
            return await _context.MaterialCategories
                .Where(x => x.is_active && !x.is_deleted)
                .OrderBy(x => x.category_name)
                .ToListAsync();
        }

        public async Task<MaterialCategory?> GetByIdAsync(int id)
        {
            return await _context.MaterialCategories
                .FirstOrDefaultAsync(x =>
                    x.material_category_id == id &&
                    !x.is_deleted);
        }

        public async Task AddAsync(CreateMaterialCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.category_name))
                throw new Exception("Category name is required.");

            var exists = await _context.MaterialCategories
                .AnyAsync(x =>
                    x.category_name == dto.category_name.Trim() &&
                    !x.is_deleted);

            if (exists)
                throw new Exception("Category already exists.");

            var category = new MaterialCategory
            {
                category_name = dto.category_name.Trim(),
                description = dto.description,
                is_active = true,
                is_deleted = false,
                created_at = DateTime.UtcNow
            };

            _context.MaterialCategories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, CreateMaterialCategoryDto dto)
        {
            var category = await _context.MaterialCategories
                .FirstOrDefaultAsync(x =>
                    x.material_category_id == id &&
                    !x.is_deleted);

            if (category == null)
                throw new Exception("Material category not found.");

            if (string.IsNullOrWhiteSpace(dto.category_name))
                throw new Exception("Category name is required.");

            var exists = await _context.MaterialCategories
                .AnyAsync(x =>
                    x.material_category_id != id &&
                    x.category_name == dto.category_name.Trim() &&
                    !x.is_deleted);

            if (exists)
                throw new Exception("Category already exists.");

            category.category_name = dto.category_name.Trim();
            category.description = dto.description;
            category.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var category = await _context.MaterialCategories
                .FirstOrDefaultAsync(x =>
                    x.material_category_id == id &&
                    !x.is_deleted);

            if (category == null)
                return false;

            category.is_deleted = true;
            category.is_active = false;
            category.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}