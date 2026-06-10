using inventory_api.DTOs.Purchasing.QcInspections;
using inventory_api.Services.Purchasing.QcInspections;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.QcInspections
{
    [ApiController]
    [Route("api/purchasing/qc-inspections")]
    public class QcInspectionController : ControllerBase
    {
        private readonly QcInspectionService _service;

        public QcInspectionController(QcInspectionService service)
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
                return NotFound(new { message = "QC inspection not found." });

            return Ok(data);
        }

        [HttpPost("{id}/save-inspection")]
        public async Task<IActionResult> SaveInspection(int id, [FromBody] SaveQcInspectionDto dto)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value
                             ?? User.FindFirst("UserId")?.Value
                             ?? "";

                await _service.SaveInspectionAsync(id, dto, userId);

                return Ok(new
                {
                    message = "QC inspection saved successfully."
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

        [HttpPost("{id}/commit")]
        public async Task<IActionResult> Commit(int id)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value
                             ?? User.FindFirst("UserId")?.Value
                             ?? "";

                await _service.CommitAsync(id, userId);

                return Ok(new
                {
                    message = "QC inspection committed to inventory successfully."
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