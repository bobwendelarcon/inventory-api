using inventory_api.Services.Reports.TimeInMotion;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Reports.TimeInMotion
{
    [ApiController]
    [Route("api/reports/time-in-motion")]
    public class TimeInMotionController : ControllerBase
    {
        private readonly TimeInMotionService _service;

        public TimeInMotionController(
            TimeInMotionService service)
        {
            _service = service;
        }


        // ============================================================
        // GET TIME IN MOTION REPORT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result =
                    await _service.GetAllAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = ex.Message
                    }
                );
            }
        }
    }
}