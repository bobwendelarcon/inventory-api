using inventory_api.DTOs.Inventory.MaterialRequisitions;
using inventory_api.Services;
using inventory_api.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace inventory_api.Controllers.Inventory
{
    [ApiController]
    [Authorize]
    [Route("api/inventory/material-requisitions")]
    public class MaterialRequisitionController : ControllerBase
    {
        private readonly MaterialRequisitionService _service;
        private readonly MrsPermissionService _permissionService;

        public MaterialRequisitionController(
            MaterialRequisitionService service,
            MrsPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
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

                if (!await _permissionService.HasPermissionAsync(
        User, "MRS_CREATE"))
                {
                    return Forbid();
                }

                var loggedInUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                // Use the authenticated Production user's ID.
                dto.CreatedBy = loggedInUserId;

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



        // =========================================================
        // PRODUCTION - PREVIEW AUTO FEFO ALLOCATION
        // =========================================================
        [HttpPost("preview-fefo")]
        public async Task<IActionResult> PreviewFefo(
            [FromBody] PreviewMaterialFefoDto dto)
        {
            try
            {

                if (!await _permissionService.HasPermissionAsync(
        User, "MRS_CREATE"))
                {
                    return Forbid();
                }
                var result =
                    await _service.PreviewFefoAllocationAsync(dto);

                return Ok(new
                {
                    success = true,
                    data = result
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to preview material allocation."
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


                bool canViewLots =
                    await _permissionService.HasPermissionAsync(
                        User, "MRS_CREATE")
                    ||
                    await _permissionService.HasPermissionAsync(
                        User, "MRS_ALLOCATE")
                    ||
                    await _permissionService.HasPermissionAsync(
                        User, "MRS_RELEASE");


                if (!canViewLots)
                {
                    return Forbid();
                }
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




        // =========================================================
        // GET ALL REQUISITIONS
        // =========================================================


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                if (!await _permissionService.HasPermissionAsync(
                    User, "INV_MATERIAL_REQUISITION"))
                {
                    return Forbid();
                }

                var canAllocate =
                    await _permissionService.HasPermissionAsync(
                        User, "MRS_ALLOCATE");

                var canStaffApprove =
                    await _permissionService.HasPermissionAsync(
                        User, "MRS_STAFF_APPROVE");

                var canHeadApprove =
                    await _permissionService.HasPermissionAsync(
                        User, "MRS_HEAD_APPROVE");

                var result = await _service.GetAllAsync(
                    canAllocate,
                    canStaffApprove,
                    canHeadApprove);

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



        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
    int id)
        {
            try
            {

                if (!await _permissionService.HasPermissionAsync(
        User, "INV_MATERIAL_REQUISITION"))
                {
                    return Forbid();
                }
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





        [HttpGet("notification-counts")]
        public async Task<IActionResult> GetNotificationCounts()
        {
            // Require access to the Material Requisition module.
            if (!await _permissionService.HasPermissionAsync(
                User, "INV_MATERIAL_REQUISITION"))
            {
                return Forbid();
            }

            // Determine the user's actionable permissions.
            var canAllocate =
                await _permissionService.HasPermissionAsync(
                    User, "MRS_ALLOCATE");

            var canStaffApprove =
                await _permissionService.HasPermissionAsync(
                    User, "MRS_STAFF_APPROVE");

            var canHeadApprove =
                await _permissionService.HasPermissionAsync(
                    User, "MRS_HEAD_APPROVE");

            var result = await _service.GetNotificationCountsAsync(
                canAllocate,
                canStaffApprove,
                canHeadApprove);

            return Ok(result);
        }


        // =========================================================
        // RECOVER PENDING MRS NOTIFICATIONS
        // =========================================================

        [HttpGet("pending-notifications")]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetPendingNotifications()
        {
            try
            {
                if (!await _permissionService.HasPermissionAsync(
                    User, "INV_MATERIAL_REQUISITION"))
                {
                    return Forbid();
                }

                if (!await _permissionService.HasPermissionAsync(
                    User, "MRS_RELEASE"))
                {
                    return Forbid();
                }

                var result =
                    await _service.GetPendingNotificationsAsync();

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to retrieve pending MRS notifications."
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
                if (!await _permissionService.HasPermissionAsync(
                        User, "MRS_CREATE"))
                {
                    return Forbid();
                }

                var loggedInUserId =
     User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                await _service.SubmitForApprovalAsync(
                    id,
                    loggedInUserId);

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
        [NonAction]
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


                var requisition = await _service.GetByIdAsync(id);

                if (requisition == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Material requisition was not found."
                    });
                }

                string requiredPermission;

                if (requisition.Status == "FOR_STAFF_APPROVAL")
                {
                    requiredPermission = "MRS_STAFF_APPROVE";
                }
                else if (requisition.Status == "FOR_HEAD_APPROVAL")
                {
                    requiredPermission = "MRS_HEAD_APPROVE";
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "MRS is not awaiting approval."
                    });
                }

                if (!await _permissionService.HasPermissionAsync(
                        User, requiredPermission))
                {
                    return Forbid();
                }




                var loggedInUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                await _service.RejectAsync(
                    id,
                    loggedInUserId,
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



                if (!await _permissionService.HasPermissionAsync(
                        User, "MRS_RELEASE"))
                {
                    return Forbid();
                }


                var loggedInUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                // Use the authenticated warehouse user.
                dto.ReleasedBy = loggedInUserId;
                dto.PostedBy = loggedInUserId;

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



        // =========================================================
        // WAREHOUSE - CONFIRM PHYSICAL ALLOCATION
        // =========================================================
        [HttpPost("{id:int}/confirm-warehouse-allocation")]
        public async Task<IActionResult> ConfirmWarehouseAllocation(
            int id,
            [FromBody] ConfirmMaterialAllocationDto dto)
        {
            try
            {

                if (!await _permissionService.HasPermissionAsync(
        User, "MRS_ALLOCATE"))
                {
                    return Forbid();
                }

                var loggedInUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                // Always use the authenticated warehouse user's ID.
                dto.ConfirmedBy = loggedInUserId;

                await _service.ConfirmWarehouseAllocationAsync(id, dto);


                return Ok(new
                {
                    success = true,
                    message = "Warehouse allocation confirmed successfully."
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to confirm warehouse allocation."
                });
            }
        }

        // =========================================================
        // WAREHOUSE STAFF - FIRST APPROVAL
        // =========================================================
        [HttpPost("{id:int}/staff-approve")]
        public async Task<IActionResult> ApproveByStaff(
            int id,
            [FromBody] ApproveMaterialRequisitionDto dto)
        {
            try
            {
                if (!await _permissionService.HasPermissionAsync(
        User, "MRS_STAFF_APPROVE"))
                {
                    return Forbid();
                }

                var loggedInUserId =
     User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                await _service.ApproveByStaffAsync(
                    id, loggedInUserId, dto.Remarks);

                return Ok(new
                {
                    success = true,
                    message = "MRS approved by Warehouse Staff."
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to approve MRS by Warehouse Staff."
                });
            }
        }

        // =========================================================
        // WAREHOUSE HEAD - FINAL APPROVAL
        // =========================================================
        [HttpPost("{id:int}/head-approve")]
        public async Task<IActionResult> ApproveByHead(
            int id,
            [FromBody] ApproveMaterialRequisitionDto dto)
        {
            try
            {
                if (!await _permissionService.HasPermissionAsync(
        User, "MRS_HEAD_APPROVE"))
                {
                    return Forbid();
                }

                var loggedInUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                await _service.ApproveByHeadAsync(
                    id, loggedInUserId, dto.Remarks);

                return Ok(new
                {
                    success = true,
                    message = "MRS approved by Warehouse Head."
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to approve MRS by Warehouse Head."
                });
            }
        }




        // =========================================================
        // WAREHOUSE - FINAL PHYSICAL PREPARATION
        // =========================================================
        [HttpPost("{id:int}/prepare")]
        public async Task<IActionResult> PrepareWarehouseAllocation(
            int id,
            [FromBody] ConfirmMaterialAllocationDto dto)
        {
            try
            {
                if (!await _permissionService.HasPermissionAsync(
                    User, "MRS_RELEASE"))
                {
                    return Forbid();
                }

                var loggedInUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(loggedInUserId))
                {
                    return Unauthorized();
                }

                dto.ConfirmedBy = loggedInUserId;

                await _service.PrepareWarehouseAllocationAsync(id, dto);

                return Ok(new
                {
                    success = true,
                    message = "Warehouse materials prepared successfully."
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to prepare warehouse materials."
                });
            }
        }



    }
}