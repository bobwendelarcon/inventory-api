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
        public async Task<IActionResult> GetInventoryDisplay(
    int page = 1,
    int pageSize = 30,
    string lot_no = "",
    string product = "",
    string warehouse = "",
    string stockStatus = "",
    string expiryStatus = "",
    string months = "",
    string from = "",
    string to = "",
    string order = "desc"
)
        {
            try
            {
                var result = await _inventoryDisplayService.GetAllAsync(
                    page,
                    pageSize,
                    lot_no,
                    product,
                    warehouse,
                    stockStatus,
                    expiryStatus,
                    months,
                    from,
                    to,
                    order
                );

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