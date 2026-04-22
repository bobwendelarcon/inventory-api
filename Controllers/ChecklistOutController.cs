using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChecklistOutController : ControllerBase
    {
        private readonly ChecklistOutService _service;

        public ChecklistOutController(ChecklistOutService service)
        {
            _service = service;
        }

        [HttpGet("checklists")]
        public async Task<IActionResult> GetChecklists()
        {
            try
            {
                var result = await _service.GetChecklistsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("checklist-details/{checklistId}")]
        public async Task<IActionResult> GetChecklistDetails(long checklistId)
        {
            try
            {
                var result = await _service.GetChecklistDetailsAsync(checklistId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("release")]
        public async Task<IActionResult> Release([FromBody] ChecklistOutRequestDto dto)
        {
            try
            {
                var result = await _service.ReleaseAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("reopen/{checklistId}")]
        public async Task<IActionResult> Reopen(long checklistId)
        {
            try
            {
                var result = await _service.ReopenAsync(checklistId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
    }
}