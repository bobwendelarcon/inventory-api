using inventory_api.DTOs.Purchasing;
using inventory_api.Services.Purchasing;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing
{
    [ApiController]
    [Route("api/purchasing/mprf")]
    public class MprfController : ControllerBase
    {
        private readonly MprfService _service;

        public MprfController(MprfService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? userId)
        {
            userId ??= "";

            var result = await _service.GetAllAsync(userId);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "MPRF not found." });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMprfDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                return Ok(new { message = "MPRF created successfully.", mprf_id = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var result = await _service.SubmitAsync(id);

            if (!result)
                return NotFound(new { message = "MPRF not found." });

            return Ok(new { message = "MPRF submitted successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "MPRF not found." });

            return Ok(new { message = "MPRF deleted successfully." });
        }

        //update
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMprfDto dto)
        {
            try
            {
                var userId = dto.requested_by;

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized("User account not found.");

                var updated = await _service.UpdateAsync(id, dto, userId);

                if (!updated)
                    return NotFound("MPRF not found.");

                return Ok(new { message = "MPRF updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //review of mprf

        [HttpGet("review-list")]
        public async Task<IActionResult> GetReviewList()
        {
            var data = await _service.GetReviewListAsync();
            return Ok(data);
        }

        [HttpPost("{id}/review")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewMprfDto dto)
        {
            try
            {
                var result = await _service.ReviewAsync(id, dto);

                if (!result)
                    return NotFound("MPRF not found.");

                return Ok(new { message = "MPRF reviewed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}