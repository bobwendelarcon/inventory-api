using Google.Api;
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreatePartnerDto dto)
        {
            await _partnerService.UpdateAsync(id, dto);
            return Ok(new { message = "Partner updated successfully." });
        }


        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
    [FromQuery] string? search,
    [FromQuery] string? type,
    [FromQuery] string? region,
    [FromQuery] bool? isDeleted,
    [FromQuery] string? agentId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] string sort = "partner_id_asc")
        {
            var result = await _partnerService.GetPagedAsync(
                search, type, region, isDeleted, agentId, page, pageSize, sort);

            return Ok(result);
        }


        [HttpDelete("reset")]
public async Task<IActionResult> ResetAll()
{
    await _partnerService.ResetAllAsync();
    return Ok(new { message = "All Partners deleted" });
}
    }
}