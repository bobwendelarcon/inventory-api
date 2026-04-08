using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            var categories = await _context.Categories
                .OrderBy(x => x.catg_name)
                .ToListAsync();

            return categories.Select(x => new Dictionary<string, object>
            {
                { "catg_id", x.catg_id },
                { "catg_name", x.catg_name },
                { "catg_desc", x.catg_desc ?? "" },
                { "is_deleted", x.is_deleted },
                { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "doc_id", x.catg_id }
            }).ToList();
        }

        public async Task AddAsync(CreateCategoryDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            bool exists = await _context.Categories.AnyAsync(x => x.catg_id == dto.catg_id);

            if (exists)
                throw new Exception("Category already exists.");

            var category = new Category
            {
                catg_id = dto.catg_id,
                catg_name = dto.catg_name,
                catg_desc = dto.catg_desc,
                is_deleted = false,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(string id, CreateCategoryDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            var category = await _context.Categories.FirstOrDefaultAsync(x => x.catg_id == id);

            if (category == null)
                throw new Exception("Category not found.");

            category.catg_name = dto.catg_name;
            category.catg_desc = dto.catg_desc ?? "";
            category.is_deleted = dto.is_deleted;
            category.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}