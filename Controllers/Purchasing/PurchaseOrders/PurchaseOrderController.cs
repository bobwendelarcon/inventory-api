using inventory_api.DTOs.Purchasing.PurchaseOrders;
using inventory_api.Services.Purchasing.PurchaseOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace inventory_api.Controllers.Purchasing.PurchaseOrders
{
    
    [ApiController]
    [Route("api/purchasing/purchase-orders")]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly PurchaseOrderService _service;

        public PurchaseOrderController(PurchaseOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        private string? GetUserId()
        {
            return
                Request.Headers["X-User-Id"]
                    .FirstOrDefault()
                ?? User.FindFirstValue("user_id")
                ?? User.FindFirstValue("UserId")
                ?? User.FindFirstValue("userId")
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.Identity?.Name;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = "Purchase Order not found." });

            return Ok(data);
        }

        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber()
        {
            var poNo = await _service.GeneratePoNoAsync();

            return Ok(new
            {
                po_no = poNo
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(
     [FromBody] CreatePurchaseOrderDto dto)
        {
            try
            {
                var userId =
                    !string.IsNullOrWhiteSpace(dto.CreatedBy)
                        ? dto.CreatedBy.Trim()
                        : GetUserId();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        message = "User ID is required."
                    });
                }

                var poId =
                    await _service.CreateAsync(
                        dto,
                        userId.Trim()
                    );

                return Ok(new
                {
                    message =
                        "Purchase Order created successfully.",
                    po_id = poId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("create-options/{canvassId}")]
        public async Task<IActionResult> GetCreateOptions(int canvassId)
        {
            var data = await _service.GetCreateOptionsAsync(canvassId);

            if (data == null)
                return NotFound(new { message = "Canvassing record not found." });

            return Ok(data);
        }

        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitForApproval(int id)
        {
            try
            {
                await _service.SubmitForApprovalAsync(id);

                return Ok(new
                {
                    message = "Purchase Order submitted for approval."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var userId =
                    GetUserId();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        message = "User ID is required."
                    });
                }

                await _service.ApproveAsync(
                    id,
                    userId.Trim()
                );

                return Ok(new
                {
                    message =
                        "Purchase Order approved successfully."
                });
            }
            catch (Exception ex)
            {
                var deepestException = ex;

                while (deepestException.InnerException != null)
                {
                    deepestException = deepestException.InnerException;
                }

                return BadRequest(new
                {
                    message = deepestException.Message
                });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _service.CancelAsync(id);

                return Ok(new
                {
                    message = "Purchase Order cancelled successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}