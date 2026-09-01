using inventory_api.DTOs;
using inventory_api.Models;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _userService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _userService.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "User not found." });

            return Ok(result);
        }

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
                    message =
                        "Username and password are required."
                });
            }

            var user =
                await _userService.LoginAsync(
                    dto.username,
                    dto.password
                );

            if (user == null)
            {
                return Unauthorized(new
                {
                    message =
                        "Invalid username or password"
                });
            }

            var accessCodes =
                await _userService
                    .GetUserAccessCodesAsync(
                        user.user_id
                    );

            return Ok(new
            {
                user.user_id,
                user.full_name,
                user.username,
                user.role_name,
                user.profile_image,

                access_points =
                    accessCodes
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            await _userService.AddAsync(dto);
            return Ok(new { message = "User added successfully" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateUserDto dto)
        {
            await _userService.UpdateAsync(id, dto);
            return Ok(new { message = "User updated successfully." });
        }

        [HttpPut("UpdateMyAccount/{id}")]
        public async Task<IActionResult> UpdateMyAccount(string id, [FromBody] UpdateMyAccountDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid request." });

            await _userService.UpdateMyAccountAsync(id, dto);
            return Ok(new { message = "Account updated successfully." });
        }


        [HttpPost("UploadProfileImage/{id}")]
        public async Task<IActionResult> UploadProfileImage(string id, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");

                var imagePath = await _userService.UploadProfileImageAsync(id, file);

                return Ok(new
                {
                    message = "Success",
                    profile_image = imagePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message); // 🔥 IMPORTANT
            }
        }

        [HttpDelete("reset")]
        public async Task<IActionResult> ResetAll()
        {
            await _userService.ResetAllAsync();
            return Ok(new { message = "All User deleted" });
        }

        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordDto dto)
        {
            try
            {
                await _userService.ChangePasswordAsync(id, dto);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

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

        [HttpGet("{id}/access-codes")]
        public async Task<IActionResult> GetUserAccessCodes(string id)
        {
            var result =
                await _userService.GetUserAccessCodesAsync(id);

            return Ok(result);
        }

        [HttpGet("available-access-points")]
        public async Task<IActionResult> GetAvailableAccessPoints()
        {
            var result =
                await _userService.GetAllAccessPointsAsync();

            return Ok(result);
        }

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