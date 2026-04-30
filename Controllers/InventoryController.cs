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
            return Ok(new
            {
                success = true,
                message = "Stock IN saved"
            });
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
    string full_name = "",
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
                full_name,
                reference,
                warehouse,
                order
            );

            return Ok(result);
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

        [HttpPut("UpdateReference")]
        public async Task<IActionResult> UpdateReference([FromBody] UpdateTransactionReferenceDto dto)
        {
            try
            {
                await _service.UpdateReferenceAsync(dto);
                return Ok(new { success = true, message = "Reference updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto dto)
        {
            await _service.TransferAsync(dto);
            return Ok("Transfer successful");
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(string product_id, string lot_no, string branch_id)
        {
            try
            {
                var result = await _service.GetHistoryByLotAsync(product_id, lot_no, branch_id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] InventoryAdjustRequestDto dto)
        {
            try
            {
                var result = await _service.AdjustAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // for return search 

        [HttpGet("search-out-for-return")]
        public async Task<IActionResult> SearchOutForReturn(
    string search = "",
    string lotNo = "",
    int limit = 50)
        {
            try
            {
                var result = await _service.SearchOutTransactionsForReturnAsync(search, lotNo, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


   


    }
}