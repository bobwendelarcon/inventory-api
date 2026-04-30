using inventory_api.DTOs.Reports;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _service;

        public ReportsController(ReportService service)
        {
            _service = service;
        }

        [HttpGet("delivery-kpi")]
        public async Task<IActionResult> GetDeliveryKpi([FromQuery] ReportFilterDto filter)
        {
            var result = await _service.GetDeliveryKpiAsync(filter);
            return Ok(result);
        }

        [HttpGet("near-expiry")]
        public async Task<IActionResult> GetNearExpiry([FromQuery] ReportFilterDto filter)
        {
            var result = await _service.GetNearExpiryAsync(filter);
            return Ok(result);
        }

        [HttpGet("returns")]
        public async Task<IActionResult> GetReturns([FromQuery] ReportFilterDto filter)
        {
            var result = await _service.GetReturnsAsync(filter);
            return Ok(result);
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory([FromQuery] ReportFilterDto filter)
        {
            var result = await _service.GetInventoryAsync(filter);
            return Ok(result);
        }
    }
}