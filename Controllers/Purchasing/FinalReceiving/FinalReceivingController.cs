using inventory_api.DTOs.Purchasing.FinalReceiving;
using inventory_api.Services.Purchasing.FinalReceiving;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace inventory_api.Controllers.Purchasing.FinalReceiving
{
    [ApiController]
    [Route("api/inventory/final-receiving")]
    public class FinalReceivingController
        : ControllerBase
    {
        private readonly FinalReceivingService
            _service;


        public FinalReceivingController(
            FinalReceivingService service)
        {
            _service = service;
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
                ?? User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.Identity?.Name;
        }


        // ============================================================
        // CREATE FINAL RR
        // ============================================================

        [HttpPost(
            "processing/{processingId:int}"
        )]
        public async Task<IActionResult>
            CreateFinalReceiving(
                int processingId,
                [FromBody]
                CreateFinalReceivingDto dto)
        {
            try
            {
                var userId =
                    GetUserId();


                if (string.IsNullOrWhiteSpace(
                    userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "User ID is required."
                    });
                }


                var result =
                    await _service
                        .CreateFinalReceivingAsync(
                            processingId,
                            dto,
                            userId.Trim()
                        );


                return Ok(new
                {
                    finalRrId =
                        result.FinalRrId,

                    finalRrNo =
                        result.FinalRrNo,

                    status =
                        result.Status,

                    message =
                        "Final Receiving Report created successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }


        // ============================================================
        // COMMIT FINAL RR TO INVENTORY
        // ============================================================

        [HttpPost("processing/{processingId:int}/commit")]
        public async Task<IActionResult> CommitFinalReceiving(
            int processingId,
            [FromBody] CommitFinalReceivingDto dto)
        {
            try
            {
                var userId =
                    GetUserId();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "User ID is required."
                    });
                }

                await _service.CommitFinalReceivingAsync(
                    processingId,
                    dto,
                    userId.Trim()
                );

                return Ok(new
                {
                    message =
                        "Final Receiving Report completed and inventory committed successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
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