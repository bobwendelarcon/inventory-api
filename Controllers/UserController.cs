using inventory_api.DTOs;
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

        //[HttpGet("barcode/{barcode}")]
        //public async Task<IActionResult> GetByBarcode(string barcode)
        //{
        //    var result = await _userService.GetByBarcodeAsync(barcode);

        //    if (result == null)
        //        return NotFound(new { message = "Product not found" });

        //    return Ok(result);
        //}

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            await _userService.AddAsync(dto);
            return Ok(new { message = "User added successfully" });
        }

        //[HttpDelete("{productId}")]
        //public async Task<IActionResult> Delete(string productId)
        //{
        //    var result = await _userService.SoftDeleteAsync(productId);

        //    if (!result)
        //        return NotFound(new { message = "Product not found" });

        //    return Ok(new { message = "Product deleted successfully" });
        //}

        [HttpDelete("reset")]
public async Task<IActionResult> ResetAll()
{
    await _userService.ResetAllAsync();
    return Ok(new { message = "All User deleted" });
}
    }
}