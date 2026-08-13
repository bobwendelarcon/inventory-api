using inventory_api.DTOs.Inventory.MaterialRequisitions;
using inventory_api.Services.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/material-requisitions")]
    public class MaterialRequisitionController : ControllerBase
    {
        private readonly MaterialRequisitionService _service;

        public MaterialRequisitionController(
            MaterialRequisitionService service)
        {
            _service = service;
        }

        // =========================================================
        // CREATE DRAFT
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreateDraft(
            [FromBody] CreateMaterialRequisitionDto dto)
        {
            try
            {
                var requisitionId =
                    await _service.CreateDraftAsync(dto);

                return Ok(new
                {
                    success = true,
                    requisitionId,
                    message =
                        "Material requisition created successfully."
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