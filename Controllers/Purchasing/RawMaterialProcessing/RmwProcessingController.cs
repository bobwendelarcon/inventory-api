using inventory_api.DTOs.Purchasing.RawMaterialProcessing;
using inventory_api.Services.Purchasing.RawMaterialProcessing;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace inventory_api.Controllers.Purchasing.RawMaterialProcessing
{
    [ApiController]
    [Route("api/inventory/raw-material-processing")]
    public class RmwProcessingController : ControllerBase
    {
        private readonly RmwProcessingService _service;

        public RmwProcessingController(
            RmwProcessingService service)
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


        [HttpPost(
            "lines/{id:int}/start-weighing"
        )]
        public async Task<IActionResult>
            StartWeighing(int id)
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

                await _service.StartWeighingAsync(
                    id,
                    userId.Trim()
                );

                return Ok(new
                {
                    message =
                        "Weighing started successfully."
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


        [HttpPost(
            "lines/{id:int}/complete-weighing"
        )]
        public async Task<IActionResult>
            CompleteWeighing(
                int id,
                [FromBody] CompleteWeighingDto dto)
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

                await _service
                    .CompleteWeighingAsync(
                        id,
                        dto,
                        userId.Trim()
                    );

                return Ok(new
                {
                    message =
                        "Weighing completed successfully."
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


        [HttpPost(
            "lines/{id:int}/complete-sticker"
        )]
        public async Task<IActionResult>
            CompleteSticker(int id)
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

                await _service.CompleteStickerAsync(
                    id,
                    userId.Trim()
                );

                return Ok(new
                {
                    message =
                        "Sticker / identification completed successfully."
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


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data =
                await _service.GetAllAsync();

            return Ok(data);
        }
    }
}