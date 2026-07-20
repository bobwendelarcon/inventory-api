using inventory_api.DTOs.Purchasing.PurchaseOrders;
using inventory_api.DTOs.Purchasing.ReceivingReports;
using inventory_api.Services.Purchasing.ReceivingReports;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.ReceivingReports
{
    [ApiController]
    [Route("api/purchasing/receiving-reports")]
    public class ReceivingReportController : ControllerBase
    {
        private readonly ReceivingReportService _service;

        public ReceivingReportController(ReceivingReportService service)
        {
            _service = service;
        }

        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber()
        {
            var rrNo = await _service.GenerateRrNoAsync();

            return Ok(new
            {
                rr_no = rrNo
            });
        }

        [HttpGet("create-options/{scheduleId}")]
        public async Task<IActionResult> GetCreateOptions(int scheduleId)
        {
            try
            {
                var data = await _service.GetCreateOptionsAsync(scheduleId);

                if (data == null)
                {
                    return NotFound(new
                    {
                        message = "Delivery schedule not found."
                    });
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.GetBaseException().Message
                });
            }
        }



        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReceivingReportDto dto)
        {
            try
            {
                var userId = !string.IsNullOrWhiteSpace(dto.CreatedBy)
                    ? dto.CreatedBy
                    : User.FindFirst("user_id")?.Value
                      ?? User.FindFirst("UserId")?.Value
                      ?? "";

                var rrId = await _service.CreateAsync(dto, userId);

                return Ok(new
                {
                    message = "Receiving Report created successfully.",
                    rr_id = rrId
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

      
     

 

 

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = "Receiving Report not found." });

            return Ok(data);
        }

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar(
    [FromQuery] DateTime start,
    [FromQuery] DateTime end)
        {
            var data = await _service.GetReceivingCalendarAsync(start, end);

            return Ok(data);
        }



        [HttpGet("schedules/{scheduleId}")]
        public async Task<IActionResult> GetScheduleDetails(
    int scheduleId)
        {
            try
            {
                var data =
                    await _service.GetScheduleDetailsAsync(
                        scheduleId
                    );

                if (data == null)
                {
                    return NotFound(new
                    {
                        message = "Delivery schedule not found."
                    });
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.GetBaseException().Message
                });
            }
        }

        [HttpPost("schedules/{scheduleId}/reschedule-remaining")]
        public async Task<IActionResult> RescheduleRemaining(
    int scheduleId,
    [FromBody] RescheduleRemainingDeliveryDto dto)
        {
            try
            {
                var userId =
                    !string.IsNullOrWhiteSpace(dto.CreatedBy)
                        ? dto.CreatedBy
                        : User.FindFirst("user_id")?.Value
                          ?? User.FindFirst("UserId")?.Value
                          ?? "";

                var newScheduleIds =
    await _service.RescheduleRemainingAsync(
        scheduleId,
        dto,
        userId
    );

                return Ok(new
                {
                    message =
                        newScheduleIds.Count == 1
                            ? "Remaining delivery rescheduled successfully."
                            : $"{newScheduleIds.Count} delivery schedules created successfully.",

                    schedule_ids = newScheduleIds
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
    }
}