using inventory_api.DTOs.Purchasing.Suppliers.Manufacturers;
using inventory_api.Services.Purchasing.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.Suppliers
{
    [ApiController]
    [Route("api/purchasing/manufacturers")]
    public class ManufacturerController : ControllerBase
    {
        private readonly ManufacturerService _manufacturerService;

        public ManufacturerController(ManufacturerService manufacturerService)
        {
            _manufacturerService = manufacturerService;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup(string? search)
        {
            var result = await _manufacturerService.LookupAsync(search);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateManufacturerDto dto)
        {
            var id = await _manufacturerService.CreateAsync(dto);

            return Ok(new
            {
                success = true,
                manufacturerId = id
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _manufacturerService.GetDetailsByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Manufacturer not found." });

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateManufacturerDto dto)
        {
            try
            {
                var updated = await _manufacturerService.UpdateAsync(id, dto);

                if (!updated)
                    return NotFound(new { message = "Manufacturer not found." });

                return Ok(new { message = "Manufacturer updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


    }
}