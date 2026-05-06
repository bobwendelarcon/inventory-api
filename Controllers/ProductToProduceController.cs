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

        [HttpGet]
        public async Task<IActionResult> GetList(
    int page = 1,
    int pageSize = 50,
    string status = "ACTIVE",
    string search = "")
        {
            var result = await _service.GetListAsync(page, pageSize, status, search);
            return Ok(result);
        }
        [HttpDelete("line/{ptpLineId}")]
        public async Task<IActionResult> DeleteLine(long ptpLineId)
        {
            await _service.DeleteLineAsync(ptpLineId);

            return Ok(new
            {
                message = "PTP request deleted successfully."
            });
        }

        [HttpPost("line/{ptpLineId}/start")]
        public async Task<IActionResult> StartProduction(long ptpLineId)
        {
            await _service.StartProductionAsync(ptpLineId);

            return Ok(new
            {
                message = "Production started successfully."
            });
        }

        [HttpPost("produce")]
        public async Task<IActionResult> Produce([FromBody] ProduceStockDto dto)
        {
            var producedBy =
     string.IsNullOrWhiteSpace(dto.producedBy)
         ? "production"
         : dto.producedBy;

            await _service.ProduceStockAsync(dto, producedBy);

            return Ok(new
            {
                message = "Production stock recorded successfully."
            });
        }




    }
}