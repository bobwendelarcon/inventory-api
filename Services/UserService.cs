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

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => !u.is_deleted)
                .ToListAsync();
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            username = username?.Trim() ?? "";
            password = password?.Trim() ?? "";

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.username == username &&
                    !u.is_deleted);

            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.password_hash))
                return null;

            return user;
        }
        public async Task AddAsync(CreateUserDto dto)
        {
            var user = new User
            {
                user_id = dto.user_id,
                full_name = dto.full_name,
                username = dto.username,
                password_hash = BCrypt.Net.BCrypt.HashPassword(dto.password_hash),
                role_name = dto.role_name,
                is_deleted = false,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(string id, CreateUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_id == id);

            if (user == null)
                throw new Exception("User not found.");

            user.full_name = dto.full_name;
            user.username = dto.username;
            if (!string.IsNullOrWhiteSpace(dto.password_hash))
            {
                user.password_hash = BCrypt.Net.BCrypt.HashPassword(dto.password_hash);
            }
            user.role_name = dto.role_name;
            user.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ResetAllAsync()
        {
            var users = await _context.Users.ToListAsync();

            foreach (var user in users)
            {
                user.is_deleted = true;
                user.updated_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}