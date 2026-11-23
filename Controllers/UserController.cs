using inventory_api.DTOs;
using inventory_api.Models;
using inventory_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace inventory_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtTokenService _jwtTokenService;

        public UserController(
    UserService userService,
    JwtTokenService jwtTokenService)
        {
            _userService = userService;
            _jwtTokenService = jwtTokenService;
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();

            var result = users.Select(u => new
            {
                u.user_id,
                u.full_name,
                u.username,
                u.role_name,
                u.profile_image,
                u.is_deleted,
                u.created_at,
                u.updated_at
            });

            return Ok(result);
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
                return NotFound(new
                {
                    message = "User not found."
                });

            return Ok(new
            {
                user.user_id,
                user.full_name,
                user.username,
                user.role_name,
                user.profile_image,
                user.is_deleted,
                user.created_at,
                user.updated_at
            });
        }


        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccount()
        {
            // Get user ID from the validated JWT.
            var loggedInUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(loggedInUserId))
                return Unauthorized();

            // Retrieve only the logged-in user's account.
            var user = await _userService.GetByIdAsync(
                loggedInUserId);

            if (user == null || user.is_deleted)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            return Ok(new
            {
                user.user_id,
                user.full_name,
                user.username,
                user.role_name,
                user.profile_image,
                user.is_deleted,
                user.created_at,
                user.updated_at
            });
        }



        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
     [FromBody] LoginRequestDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.username) ||
                string.IsNullOrWhiteSpace(dto.password))
            {
                return BadRequest(new
                {
                    message = "Username and password are required."
                });
            }

            var user = await _userService.LoginAsync(
                dto.username,
                dto.password);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password"
                });
            }

            var accessCodes =
                await _userService.GetUserAccessCodesAsync(
                    user.user_id);

            // Generate JWT token after successful login
            var token = _jwtTokenService.GenerateToken(
                user.user_id,
                user.username,
                user.role_name);

            return Ok(new
            {
                user.user_id,
                user.full_name,
                user.username,
                user.role_name,
                user.profile_image,

                access_points = accessCodes,

                token = token
            });
        }


        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create(
      [FromBody] CreateUserDto dto)
        {
            await _userService.AddAsync(dto);
            return Ok(new { message = "User added successfully" });
        }
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateUserDto dto)
        {
            await _userService.UpdateAsync(id, dto);
            return Ok(new { message = "User updated successfully." });
        }

        [HttpPut("UpdateMyAccount/{id}")]
        public async Task<IActionResult> UpdateMyAccount(
     string id,
     [FromBody] UpdateMyAccountDto dto)
        {
            var loggedInUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(loggedInUserId))
                return Unauthorized();

            // Users can only update their own account.
            if (!string.Equals(
                loggedInUserId,
                id,
                StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (dto == null)
                return BadRequest(new
                {
                    message = "Invalid request."
                });

            await _userService.UpdateMyAccountAsync(
                loggedInUserId,
                dto);

            return Ok(new
            {
                message = "Account updated successfully."
            });
        }

        [HttpPost("UploadProfileImage/{id}")]
        public async Task<IActionResult> UploadProfileImage(
        string id,
        IFormFile file)
        {
            var loggedInUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(loggedInUserId))
                return Unauthorized();

            // Only allow users to update their own profile image
            if (!string.Equals(
                loggedInUserId,
                id,
                StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (file == null || file.Length == 0)
                return BadRequest(new
                {
                    message = "No file uploaded."
                });

            // Maximum file size: 2 MB
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new
                {
                    message = "Maximum image size is 2 MB."
                });

            try
            {
                var imagePath =
                    await _userService.UploadProfileImageAsync(
                        loggedInUserId,
                        file);

                return Ok(new
                {
                    message = "Success",
                    profile_image = imagePath
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "Unable to upload profile image."
                });
            }
        }

        //[HttpDelete("reset")]
        //public async Task<IActionResult> ResetAll()
        //{
        //    await _userService.ResetAllAsync();
        //    return Ok(new { message = "All User deleted" });
        //}

        [NonAction]
        public IActionResult ResetAll()
        {
            return NotFound();
        }

        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(
     string id,
     [FromBody] ChangePasswordDto dto)
        {
            var loggedInUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(loggedInUserId))
                return Unauthorized();

            // Only the account owner can change their password
            if (!string.Equals(
                loggedInUserId,
                id,
                StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (dto == null)
                return BadRequest(new
                {
                    message = "Invalid request."
                });

            try
            {
                await _userService.ChangePasswordAsync(
                    loggedInUserId,
                    dto);

                return Ok(new
                {
                    message = "Password changed successfully."
                });
            }
            catch (Exception)
            {
                return BadRequest(new
                {
                    message = "Password change failed. Check your current password and request."
                });
            }
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id}/access-points")]
        public async Task<IActionResult> GetUserAccessPoints(string id)
        {
            try
            {
                var result =
                    await _userService.GetUserAccessPointsAsync(id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.GetBaseException().Message
                });
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/access-points")]
        public async Task<IActionResult> SaveUserAccessPoints(
     string id,
    [FromBody] SaveUserAccessDto dto)
        {
            try
            {
                await _userService.SaveUserAccessPointsAsync(
                    id,
                    dto
                );

                return Ok(new
                {
                    message = "User access points updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.GetBaseException().Message
                });
            }
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id}/access-codes")]
        public async Task<IActionResult> GetUserAccessCodes(string id)
        {
            var result =
                await _userService.GetUserAccessCodesAsync(id);

            return Ok(result);
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet("available-access-points")]
        public async Task<IActionResult> GetAvailableAccessPoints()
        {
            var result =
                await _userService.GetAllAccessPointsAsync();

            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var deleted =
                await _userService.DeleteUserAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            return Ok(new
            {
                message = "User account deleted successfully."
            });
        }
    }
}