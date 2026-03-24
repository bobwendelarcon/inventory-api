using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartnersController : ControllerBase
    {
        private readonly PartnerService _partnerService;

        public PartnersController(PartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _partnerService.GetAllAsync();
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
        public async Task<IActionResult> Create([FromBody] CreatePartnerDto dto)
        {
            await _partnerService.AddAsync(dto);
            return Ok(new { message = "Partner added successfully" });
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
    await _partnerService.ResetAllAsync();
    return Ok(new { message = "All Partners deleted" });
}
    }
}