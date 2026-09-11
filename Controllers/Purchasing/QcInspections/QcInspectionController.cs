using inventory_api.DTOs.Purchasing.QcInspections;
using inventory_api.Services.Purchasing.QcInspections;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace inventory_api.Controllers.Purchasing.QcInspections
{
    [ApiController]
    [Route("api/purchasing/qc-inspections")]
    public class QcInspectionController : ControllerBase
    {
        private readonly QcInspectionService _service;

        public QcInspectionController(
            QcInspectionService service)
        {
            _service = service;
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
            {
                return NotFound(new
                {
                    message = "QC inspection not found."
                });
            }

            return Ok(data);
        }

        [HttpGet("pending-evaluation")]
        public async Task<IActionResult> GetPendingEvaluation()
        {
            try
            {
                var result =
                    await _service.GetPendingRawMaterialEvaluationsAsync();

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

        [HttpPost("quarantine/{quarantineId:int}/start-evaluation")]
        public async Task<IActionResult> StartEvaluation(
    int quarantineId)
        {
            try
            {
                var userId =
                    Request.Headers["X-User-Id"].FirstOrDefault()
                    ?? User.FindFirstValue("user_id")
                    ?? User.FindFirstValue("UserId")
                    ?? User.FindFirstValue("userId")
                    ?? User.FindFirstValue("id")
                    ?? User.FindFirstValue(
                        ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? User.Identity?.Name;

                //if (string.IsNullOrWhiteSpace(userId))
                //{
                //    return Unauthorized(new
                //    {
                //        message =
                //            "The logged-in user does not have a valid user ID."
                //    });
                //}

                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = "user001"; // TEMPORARY: Swagger testing only
                }

                var result =
                    await _service
                        .StartRawMaterialEvaluationAsync(
                            quarantineId,
                            userId.Trim()
                        );

                return Ok(result);
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

        [HttpPost("{id}/save-inspection")]
        public async Task<IActionResult> SaveInspection(
            int id,
            [FromBody] SaveQcInspectionDto dto)
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

                //if (string.IsNullOrWhiteSpace(userId))
                //{
                //    return Unauthorized(new
                //    {
                //        message =
                //            "The logged-in user does not have a valid user ID."
                //    });
                //}

                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = "user001"; // TEMPORARY: Swagger testing only
                }

                await _service.SaveInspectionAsync(
                    id,
                    dto,
                    userId.Trim()
                );

                return Ok(new
                {
                    message =
                        "QC inspection saved successfully."
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

        [HttpPost("quarantine/{id:int}/release")]
        public async Task<IActionResult> ReleaseQuarantine(
    int id)
        {
            try
            {
                var userId =
                    Request.Headers["X-User-Id"]
                        .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        message = "User ID is required."
                    });
                }

                await _service.ReleaseQuarantineAsync(
                    id,
                    userId);

                return Ok(new
                {
                    message =
                        "Material released successfully."
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

        [HttpGet("rmw-material-processing")]
        public async Task<IActionResult>
    GetRmwMaterialProcessing()
        {
            var data =
                await _service
                    .GetRmwMaterialProcessingAsync();

            return Ok(data);
        }

    }
}