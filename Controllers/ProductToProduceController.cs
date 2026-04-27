using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductToProduceController : ControllerBase
    {
        private readonly ProductToProduceService _service;

        public ProductToProduceController(ProductToProduceService service)
        {
            _service = service;
        }

        [HttpGet("planning-shortages")]
        public async Task<IActionResult> GetPlanningShortages()
        {
            var result = await _service.GetPlanningShortagesAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductToProduceDto dto)
        {
            if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
                return BadRequest("No items to produce.");

            var id = await _service.CreateAsync(dto);

            return Ok(new
            {
                message = "Product to Produce request created successfully.",
                ptpId = id
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound("PTP request not found.");

            return Ok(result);
        }
    }
}