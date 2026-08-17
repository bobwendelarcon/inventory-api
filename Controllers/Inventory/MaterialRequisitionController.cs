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


        [HttpGet("available-lots")]
        public async Task<IActionResult> GetAvailableLots(
    [FromQuery] int materialId,
    [FromQuery] string branchId)
        {
            try
            {
                var result =
                    await _service.GetAvailableLotsAsync(
                        materialId,
                        branchId);

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
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message,
                    detail = ex.ToString()
                });
            }
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
    int id)
        {
            try
            {
                var result =
                    await _service.GetByIdAsync(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Material requisition was not found."
                    });
                }

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



        // =========================================================
        // SUBMIT FOR APPROVAL
        // =========================================================
        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> SubmitForApproval(
            int id,
            [FromBody] SubmitMaterialRequisitionDto dto)
        {
            try
            {
                await _service.SubmitForApprovalAsync(
                    id,
                    dto.SubmittedBy);

                return Ok(new
                {
                    success = true,
                    message =
                        "Material requisition submitted for approval."
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


        // =========================================================
        // APPROVE
        // =========================================================
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] ApproveMaterialRequisitionDto dto)
        {
            try
            {
                await _service.ApproveAsync(
                    id,
                    dto.ApprovedBy,
                    dto.Remarks);

                return Ok(new
                {
                    success = true,
                    message =
                        "Material requisition approved successfully."
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


        // =========================================================
        // REJECT
        // =========================================================
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(
            int id,
            [FromBody] RejectMaterialRequisitionDto dto)
        {
            try
            {
                await _service.RejectAsync(
                    id,
                    dto.RejectedBy,
                    dto.Reason);

                return Ok(new
                {
                    success = true,
                    message =
                        "Material requisition rejected."
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


        // =========================================================
        // RELEASE / STOCK OUT
        // =========================================================
        [HttpPost("{id:int}/release")]
        public async Task<IActionResult> Release(
            int id,
            [FromBody] PostMaterialRequisitionDto dto)
        {
            try
            {
                await _service.ReleaseAsync(
                    id,
                    dto);

                return Ok(new
                {
                    success = true,
                    message =
                        "Material requisition released successfully."
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