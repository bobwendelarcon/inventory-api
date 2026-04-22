using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class BranchesService
    {
        private readonly AppDbContext _context;

        public BranchesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            var branches = await _context.Branches
                .OrderBy(x => x.branch_name)
                .ToListAsync();

            return branches.Select(x => new Dictionary<string, object>
            {
                { "branch_id", x.branch_id },
                { "branch_name", x.branch_name },
                { "branch_loc", x.branch_loc ?? "" },
                { "is_deleted", x.is_deleted },
                { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
               // { "doc_id", x.branch_id }
            }).ToList();
        }

        public async Task AddAsync(CreateBranchesDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            bool exists = await _context.Branches.AnyAsync(x => x.branch_id == dto.branch_id);

            if (exists)
                throw new Exception("Branch already exists.");

            var branch = new Branch
            {
                branch_id = dto.branch_id,
                branch_name = dto.branch_name,
                branch_loc = dto.branch_loc,
                is_deleted = false,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(string branchId)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(x => x.branch_id == branchId);

            if (branch == null)
                return false;

            branch.is_deleted = true;
            branch.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateAsync(string id, CreateBranchesDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(id))
                throw new Exception("Branch id is required.");

            if (string.IsNullOrWhiteSpace(dto.branch_name))
                throw new Exception("branch_name is required.");

            var branch = await _context.Branches
                .FirstOrDefaultAsync(x => x.branch_id == id);

            if (branch == null)
                throw new Exception("Branch not found.");

            // 🔥 Update fields
            branch.branch_name = dto.branch_name;
            branch.branch_loc = dto.branch_loc;
            branch.is_deleted = dto.is_deleted;

            branch.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ResetAllAsync()
        {
            var branches = await _context.Branches.ToListAsync();

            _context.Branches.RemoveRange(branches);
            await _context.SaveChangesAsync();
        }
    }
}