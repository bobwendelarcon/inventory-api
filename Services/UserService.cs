using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            var users = await _context.Users.ToListAsync();

            return users.Select(x => new Dictionary<string, object>
            {
                { "user_id", x.user_id },
                { "full_name", x.full_name },
                { "username", x.username },
                { "password_hash", x.password_hash },
                { "role_name", x.role_name },
                { "is_deleted", x.is_deleted },
                { "created_at", x.created_at.ToString("yyyy-MM-dd HH:mm:ss") },
                { "updated_at", x.updated_at.ToString("yyyy-MM-dd HH:mm:ss") }
            }).ToList();
        }

        public async Task AddAsync(CreateUserDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.user_id))
                throw new Exception("user_id is required.");

            if (string.IsNullOrWhiteSpace(dto.full_name))
                throw new Exception("full_name is required.");

            if (string.IsNullOrWhiteSpace(dto.username))
                throw new Exception("username is required.");

            if (string.IsNullOrWhiteSpace(dto.password_hash))
                throw new Exception("password_hash is required.");

            bool exists = await _context.Users.AnyAsync(x => x.user_id == dto.user_id);

            if (exists)
                throw new Exception("User already exists.");

            var user = new User
            {
                user_id = dto.user_id,
                full_name = dto.full_name,
                username = dto.username,
                password_hash = dto.password_hash,
                role_name = dto.role_name,
                is_deleted = dto.is_deleted,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(string id, CreateUserDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (string.IsNullOrWhiteSpace(id))
                throw new Exception("user_id is required.");

            if (string.IsNullOrWhiteSpace(dto.full_name))
                throw new Exception("full_name is required.");

            if (string.IsNullOrWhiteSpace(dto.username))
                throw new Exception("username is required.");

            if (string.IsNullOrWhiteSpace(dto.password_hash))
                throw new Exception("password_hash is required.");

            var user = await _context.Users.FirstOrDefaultAsync(x => x.user_id == id);

            if (user == null)
                throw new Exception("User not found.");

            user.full_name = dto.full_name;
            user.username = dto.username;
            user.password_hash = dto.password_hash;
            user.role_name = dto.role_name;
            user.is_deleted = dto.is_deleted;
            user.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ResetAllAsync()
        {
            var users = await _context.Users.ToListAsync();

            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();
        }
    }
}