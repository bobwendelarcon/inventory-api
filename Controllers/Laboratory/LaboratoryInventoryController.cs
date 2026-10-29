
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using inventory_api.Models.Laboratory;
using inventory_api.Services.Laboratory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Laboratory
{
    [ApiController]
    [Route("api/laboratory")]
    //[Authorize(Roles = "ADMIN,STAFF,QA_QC")]
    public class LaboratoryInventoryController : ControllerBase
    {
        private readonly LaboratoryInventoryService _service;

        public LaboratoryInventoryController(
            LaboratoryInventoryService service)
        {
            _service = service;
        }

        // =====================================================
        // CATEGORY MASTER
        // =====================================================

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await _service.GetCategoriesAsync());
        }

        [HttpPost("categories")]
        public async Task<IActionResult> SaveCategory(
            [FromBody] LaboratoryCategory model)
        {
            return await ExecuteAsync(
                () => _service.SaveCategoryAsync(model));
        }

        // =====================================================
        // BRAND MASTER
        // =====================================================

        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
        {
            return Ok(await _service.GetBrandsAsync());
        }

        [HttpPost("brands")]
        public async Task<IActionResult> SaveBrand(
            [FromBody] LaboratoryBrand model)
        {
            return await ExecuteAsync(
                () => _service.SaveBrandAsync(model));
        }

        // =====================================================
        // LOCATION MASTER
        // =====================================================

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            return Ok(await _service.GetLocationsAsync());
        }

        [HttpPost("locations")]
        public async Task<IActionResult> SaveLocation(
            [FromBody] LaboratoryLocation model)
        {
            return await ExecuteAsync(
                () => _service.SaveLocationAsync(model));
        }

        // =====================================================
        // ITEM MASTER
        // =====================================================

        [HttpGet("items")]
        public async Task<IActionResult> GetItems(
            [FromQuery] string search = "")
        {
            return Ok(await _service.GetItemsAsync(search));
        }

        [HttpPost("items")]
        public async Task<IActionResult> SaveItem(
            [FromBody] LaboratoryItem model)
        {
            return await ExecuteAsync(
                () => _service.SaveItemAsync(model));
        }

        // =====================================================
        // INVENTORY LIST
        // =====================================================

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory(
            [FromQuery] string search = "",
            [FromQuery] long? categoryId = null,
            [FromQuery] long? brandId = null,
            [FromQuery] long? locationId = null,
            [FromQuery] string stockStatus = "")
        {
            return Ok(await _service.GetInventoryAsync(
                search,
                categoryId,
                brandId,
                locationId,
                stockStatus));
        }

        // =====================================================
        // CREATE INVENTORY RECORD
        // =====================================================

        [HttpPost("inventory")]
        public async Task<IActionResult> CreateInventory(
            [FromBody] LaboratoryInventory model)
        {
            return await ExecuteAsync(
                () => _service.CreateInventoryAsync(model));
        }

        // =====================================================
        // UPDATE INVENTORY DETAILS
        // =====================================================

        [HttpPut("inventory/{inventoryId:long}/details")]
        public async Task<IActionResult> UpdateInventoryDetails(
            long inventoryId,
            [FromBody] LaboratoryInventory model)
        {
            return await ExecuteAsync(
                () => _service.UpdateInventoryDetailsAsync(
                    inventoryId, model));
        }

        // =====================================================
        // STOCK IN
        // =====================================================

        [HttpPost("inventory/stock-in")]
        public async Task<IActionResult> StockIn(
            [FromBody] LaboratoryMovementRequest request)
        {
            return await ExecuteAsync(
                () => _service.StockInAsync(request));
        }

        // =====================================================
        // STOCK OUT
        // =====================================================

        [HttpPost("inventory/stock-out")]
        public async Task<IActionResult> StockOut(
            [FromBody] LaboratoryMovementRequest request)
        {
            return await ExecuteAsync(
                () => _service.StockOutAsync(request));
        }

        // =====================================================
        // INVENTORY ADJUSTMENT
        // =====================================================

        [HttpPost("inventory/adjust")]
        public async Task<IActionResult> AdjustInventory(
            [FromBody] LaboratoryMovementRequest request)
        {
            return await ExecuteAsync(
                () => _service.AdjustAsync(request));
        }

        // =====================================================
        // TRANSACTION HISTORY
        // =====================================================

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string search = "",
            [FromQuery] long? locationId = null,
            [FromQuery] string movement = "",
            [FromQuery] string referenceType = "",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            return await ExecuteAsync(
                () => _service.GetTransactionsAsync(
                    search,
                    locationId,
                    movement,
                    referenceType,
                    fromDate,
                    toDate));
        }

        // =====================================================
        // TRANSACTION SUMMARY
        // =====================================================

        [HttpGet("transactions/summary")]
        public async Task<IActionResult> GetTransactionSummary()
        {
            return Ok(
                await _service.GetTransactionSummaryAsync());
        }

        // =====================================================
        // SHARED ERROR HANDLER
        // =====================================================


        [HttpPost("items/{itemId:long}/deactivate")]
        public async Task<IActionResult> DeactivateItem(long itemId)
        {
            try
            {
                await _service.DeactivateItemAsync(itemId);

                return Ok(new
                {
                    success = true,
                    message = "Laboratory item deactivated successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        private async Task<IActionResult> ExecuteAsync<T>(
            Func<Task<T>> action)
        {
            try
            {
                var result = await action();

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
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
        }
    }
}
