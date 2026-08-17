using inventory_api.DTOs.Inventory.RawMaterials;
using inventory_api.Services.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/raw-materials")]
    public class RawMaterialInventoryController : ControllerBase
    {
        private readonly RawMaterialInventoryService _service;

        public RawMaterialInventoryController(
            RawMaterialInventoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetInventory(
            [FromQuery] RawMaterialInventoryFilterDto filter)
        {
            try
            {
                var result = await _service.GetInventoryAsync(filter);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions(
    [FromQuery] RawMaterialTransactionFilterDto filter)
        {
            try
            {
                var result =
                    await _service.GetAllTransactionsAsync(filter);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPost("manual-stock-in")]
        public async Task<IActionResult> ManualStockIn(
    [FromBody] ManualStockInDto dto)
        {
            try
            {
                await _service.ManualStockInAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Manual stock in saved successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }



        [HttpGet("{materialLotId:int}/transactions")]
        public async Task<IActionResult> GetTransactions(int materialLotId)
        {
            try
            {
                var result =
                    await _service.GetTransactionsAsync(materialLotId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet("consolidated")]
        public async Task<IActionResult>
    GetConsolidatedInventory(
        [FromQuery]
        RawMaterialConsolidatedFilterDto filter)
        {
            try
            {
                var result =
                    await _service
                        .GetConsolidatedInventoryAsync(
                            filter);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = ex.Message
                    });
            }
        }


        [HttpPost("adjust-stock")]
        public async Task<IActionResult> AdjustStock(
    [FromBody] AdjustRawMaterialStockDto dto)
        {
            try
            {
                await _service.AdjustStockAsync(dto);

                return Ok(new
                {
                    success = true,
                    message =
                        "Stock adjustment saved successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}