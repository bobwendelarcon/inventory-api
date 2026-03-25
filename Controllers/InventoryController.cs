using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Inventory Transaction")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryTransactionService _service;

        public InventoryController(InventoryTransactionService service)
        {
            _service = service;
        }

        [HttpPost("in")]
        public async Task<IActionResult> StockIn([FromBody] CreateInventoryTransactionDto dto)
        {
            dto.transaction_type = "IN";
            await _service.AddAsync(dto);
            return Ok("Stock IN saved");
        }

        [HttpPost("out")]
        public async Task<IActionResult> StockOut([FromBody] CreateInventoryTransactionDto dto)
        {
            dto.transaction_type = "OUT";
            await _service.AddAsync(dto);
            return Ok("Stock OUT saved");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }
    }
}