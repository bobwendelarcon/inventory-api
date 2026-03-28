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

        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    var result = await _service.GetAllAsync();
        //    return Ok(result);
        //}

        [HttpGet]
        public async Task<IActionResult> GetAll(int page =1, int pageSize=50)
        {
            var result = await _service.GetAllAsync();
           // return Ok(result);

            var total = result.Count;
            var pagedData = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new
            {
                total = total,
                page = page,
                pageSize = pageSize,
                result = pagedData
            });


        }

        [HttpDelete("reset")]
        public async Task<IActionResult> ResetAll()
        {
            try
            {
                await _service.ClearAllDataAsync();
                return Ok(new { success = true, message = "All data cleared" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}