using inventory_api.DTOs.Purchasing.IncomingReceiving;
using inventory_api.Services.Purchasing.IncomingReceiving;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.IncomingReceiving
{
    [ApiController]
    [Route("api/purchasing/incoming-receiving")]
    public class IncomingReceivingController : ControllerBase
    {
        private readonly IncomingReceivingService _service;

        public IncomingReceivingController(
            IncomingReceivingService service)
        {
            _service = service;
        }


        // ============================================================
        // GENERATE NEXT INCOMING RECEIVING NUMBER
        // Example: IR-2026-0001
        // ============================================================
        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber()
        {
            try
            {
                var incomingNo =
                    await _service.GenerateIncomingNoAsync();

                return Ok(new
                {
                    incoming_no = incomingNo
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.GetBaseException().Message
                });
            }
        }


        // ============================================================
        // CREATE INCOMING RECEIVING
        // Used by Android / Dashboard
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateIncomingReceivingDto dto)
        {
            try
            {
                var userId =
                    !string.IsNullOrWhiteSpace(dto.CreatedBy)
                        ? dto.CreatedBy
                        : User.FindFirst("user_id")?.Value
                          ?? User.FindFirst("UserId")?.Value
                          ?? User.Identity?.Name
                          ?? "";

                var userRole =
                    User.FindFirst(
                        System.Security.Claims.ClaimTypes.Role
                    )?.Value
                    ?? User.FindFirst("role")?.Value
                    ?? "";

                var incomingReceivingId =
                    await _service.CreateAsync(
                        dto,
                        userId,
                        userRole
                    );

                return Ok(new
                {
                    message =
                        "Incoming delivery recorded successfully.",

                    incoming_receiving_id =
                        incomingReceivingId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message =
                        ex.GetBaseException().Message
                });
            }
        }

        [HttpGet("schedule/{scheduleId:int}")]
        public async Task<IActionResult> GetSchedule(
    int scheduleId)
        {
            try
            {
                var data =
                    await _service.GetScheduleDetailsAsync(
                        scheduleId);

                if (data == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Delivery schedule not found."
                    });
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message =
                        ex.GetBaseException().Message
                });
            }
        }

        [HttpGet("manufacturers")]
        public async Task<IActionResult> GetManufacturers(
    [FromQuery] int supplierId,
    [FromQuery] int materialId)
        {
            try
            {
                var result =
                    await _service.GetManufacturersAsync(
                        supplierId,
                        materialId);

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