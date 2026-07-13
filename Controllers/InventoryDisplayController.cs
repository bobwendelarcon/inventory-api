using inventory_api.DTOs;
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

        public InventoryDisplayController(
            InventoryDisplayService inventoryDisplayService)
        {
            _inventoryDisplayService = inventoryDisplayService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetInventoryCategories()
        {
            var result =
                await _inventoryDisplayService.GetInventoryCategoriesAsync();

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetInventoryDisplay(
            int page = 1,
            int pageSize = 30,
            string lot_no = "",
            string search = "",
            string warehouse = "",
            string category = "",
            string stockStatus = "",
            string expiryStatus = "",
            string months = "",
            string from = "",
            string to = "",
            string sortBy = "lot",
            string order = "desc"
        )
        {
            try
            {
                var result = await _inventoryDisplayService.GetAllAsync(
                    page,
                    pageSize,
                    lot_no,
                    search,
                    warehouse,
                    category,
                    stockStatus,
                    expiryStatus,
                    months,
                    from,
                    to,
                    sortBy,
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

        // NEW: Grouped inventory summary for printing
        [HttpGet("print-summary")]
        public async Task<IActionResult> GetInventoryPrintSummary(
    string search = "",
    string warehouse = "",
    string categories = "",
    string stockStatus = "",
    string order = "asc")
        {
            try
            {
                var result =
                    await _inventoryDisplayService
                        .GetPrintSummaryAsync(
                            search,
                            warehouse,
                            categories,
                            stockStatus,
                            order
                        );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message =
                        "Failed to load inventory print summary.",

                    error = ex.Message
                });
            }
        }
        [HttpPut("update-lot-dates")]
        public async Task<IActionResult> UpdateLotDates(
            [FromBody] UpdateLotDatesDto dto)
        {
            try
            {
                var result =
                    await _inventoryDisplayService.UpdateLotDatesAsync(dto);

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