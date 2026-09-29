using inventory_api.DTOs.Purchasing.QaQcReceiving;
using inventory_api.Services.Purchasing.QaQcReceiving;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace inventory_api.Controllers.Purchasing.QaQcReceiving
{
    [ApiController]
    [Route("api/purchasing/qa-qc-receiving")]
    public class QaQcReceivingInspectionController : ControllerBase
    {
        private readonly QaQcReceivingInspectionService _service;

        public QaQcReceivingInspectionController(
            QaQcReceivingInspectionService service)
        {
            _service = service;
        }


        // ============================================================
        // GET NEXT INSPECTION NUMBER
        // ============================================================
        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber()
        {
            try
            {
                var inspectionNo =
                    await _service.GenerateInspectionNoAsync();

                return Ok(new
                {
                    success = true,
                    inspectionNo
                });
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


        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            try
            {
                var result =
                    await _service.GetPendingAsync();

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


        // ============================================================
        // LOAD RMW RECEIVING FOR QA/QC INSPECTION
        // ============================================================
        [HttpGet("incoming/{incomingReceivingId:int}")]
        public async Task<IActionResult> GetIncomingDetails(
            int incomingReceivingId)
        {
            try
            {
                var result =
                    await _service.GetIncomingDetailsAsync(
                        incomingReceivingId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Incoming receiving record not found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
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

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var result =
                    await _service.GetHistoryAsync();

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

        [HttpPost("save")]
        public async Task<IActionResult> SaveInspection(
       [FromBody] SaveQaQcReceivingInspectionDto dto)
        {
            try
            {
                var userId =
                    Request.Headers["X-User-Id"].FirstOrDefault()
                    ?? User.FindFirstValue("user_id")
                    ?? User.FindFirstValue("UserId")
                    ?? User.FindFirstValue("userId")
                    ?? User.FindFirstValue("id")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? User.Identity?.Name;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "QA/QC inspector user ID is required."
                    });
                }

                var qaReceivingId =
                    await _service.SaveInspectionAsync(
                        dto,
                        userId.Trim());

                return Ok(new
                {
                    success = true,
                    qaReceivingId,
                    message =
                        "QA/QC Receiving Inspection saved successfully."
                });
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