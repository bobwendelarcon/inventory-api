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
    }
}