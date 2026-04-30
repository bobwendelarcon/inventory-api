using inventory_api.Data;
using inventory_api.DTOs;
using inventory_api.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
                profile_image = null,
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

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Users
                .Where(u => u.user_id == id && !u.is_deleted)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateMyAccountAsync(string id, UpdateMyAccountDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.user_id == id && !u.is_deleted);

            if (user == null)
                throw new Exception("User not found.");

            user.full_name = dto.full_name;
            user.username = dto.username;

            if (!string.IsNullOrWhiteSpace(dto.password_hash))
            {
                user.password_hash = BCrypt.Net.BCrypt.HashPassword(dto.password_hash);
            }

            user.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<string> UploadProfileImageAsync(string id, IFormFile file)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.user_id == id && !u.is_deleted);

            if (user == null)
                throw new Exception("User not found.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                throw new Exception("Only JPG, JPEG, PNG, and WEBP files are allowed.");

            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "profile");
            Directory.CreateDirectory(uploadFolder);

            // ✅ Delete old profile image first
            if (!string.IsNullOrWhiteSpace(user.profile_image))
            {
                var oldRelativePath = user.profile_image
                    .TrimStart('/')
                    .Replace("/", Path.DirectorySeparatorChar.ToString());

                var oldFilePath = Path.Combine(_environment.WebRootPath, oldRelativePath);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            var fileName = $"{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/profile/{fileName}";

            user.profile_image = relativePath;
            user.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return relativePath;
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.user_id == userId && !u.is_deleted);

            if (user == null)
                throw new Exception("User not found.");

            if (string.IsNullOrWhiteSpace(dto.current_password))
                throw new Exception("Current password is required.");

            if (string.IsNullOrWhiteSpace(dto.new_password))
                throw new Exception("New password is required.");

            if (string.IsNullOrWhiteSpace(dto.confirm_password))
                throw new Exception("Confirm password is required.");

            if (dto.new_password != dto.confirm_password)
                throw new Exception("New password and confirm password do not match.");

            if (!BCrypt.Net.BCrypt.Verify(dto.current_password, user.password_hash))
                throw new Exception("Current password is incorrect.");

            user.password_hash = BCrypt.Net.BCrypt.HashPassword(dto.new_password);
            user.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}