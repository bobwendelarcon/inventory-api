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

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "The logged-in user does not have a valid user ID."
                    });
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
    }
}