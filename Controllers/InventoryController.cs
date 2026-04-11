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
        public async Task<IActionResult> GetAll(
    int page = 1,
    int pageSize = 30,
    string lot_no = "",
    string product = "",
    string type = "",
    string from = "",
    string to = "",
    string scanned_by = "",
    string reference = "",
    string warehouse = "",
    string order = "desc"
)
        {
            var result = await _service.GetAllAsync(
                page,
                pageSize,
                lot_no,
                product,
                type,
                from,
                to,
                scanned_by,
                reference,
                warehouse,
                order
            );

            return Ok(result);
        }
        //public async Task<IActionResult> GetAll(int page = 1, int pageSize = 30)
        //{
        //    var result = await _service.GetAllAsync();

        //    if (page < 1) page = 1;
        //    if (pageSize < 1) pageSize = 30;

        //    var total = result.Count;
        //    var pagedData = result
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToList();

        //    return Ok(new
        //    {
        //        total = total,
        //        page = page,
        //        pageSize = pageSize,
        //        data = pagedData
        //    });
        //}

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