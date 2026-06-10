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

        [HttpGet("create-options/{poId}")]
        public async Task<IActionResult> GetCreateOptions(int poId)
        {
            try
            {
                var data = await _service.GetCreateOptionsAsync(poId);

                if (data == null)
                    return NotFound(new { message = "Purchase Order not found." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/submit-qc")]
        public async Task<IActionResult> SubmitForQc(int id)
        {
            try
            {
                await _service.SubmitForQcAsync(id);
                return Ok(new { message = "RR submitted for QC." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value
                             ?? User.FindFirst("UserId")?.Value
                             ?? "";

                await _service.AcceptAsync(id, userId);

                return Ok(new { message = "RR accepted by QC." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value
                             ?? User.FindFirst("UserId")?.Value
                             ?? "";

                await _service.RejectAsync(id, userId);

                return Ok(new { message = "RR rejected by QC." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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

                return Ok(new { message = "RR committed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
    }
}