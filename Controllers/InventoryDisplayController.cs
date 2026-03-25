using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/inventoryDisplay")]
    [Tags("Inventory Display")]
    public class InventoryDisplayController : ControllerBase
    {
        private readonly InventoryDisplayService _inventoryDisplayService;

        public InventoryDisplayController(InventoryDisplayService inventoryDisplayService)
        {
            _inventoryDisplayService = inventoryDisplayService;
        }

        [HttpGet]
        public async Task<IActionResult> GetInventoryDisplay()
        {
            try
            {
                var result = await _inventoryDisplayService.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to load inventory display.",
                    error = ex.Message
                });
            }
        }
    }
}