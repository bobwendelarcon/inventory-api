using inventory_api.Services.Reports.PurchasingLeadTime;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Reports
{
    [ApiController]
    [Route("api/reports/purchasing-lead-time")]
    public class PurchasingLeadTimeController
        : ControllerBase
    {
        private readonly PurchasingLeadTimeService
            _service;

        public PurchasingLeadTimeController(
            PurchasingLeadTimeService service)
        {
            _service = service;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data =
                    await _service.GetAllAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            ex.GetBaseException()
                                .Message
                    }
                );
            }
        }
    }
}