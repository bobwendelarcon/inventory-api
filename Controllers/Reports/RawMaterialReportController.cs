using inventory_api.DTOs.Reports.RawMaterials;
using inventory_api.Services.Reports.RawMaterials;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Reports
{
    [ApiController]
    [Route("api/reports/raw-materials")]
    public class RawMaterialReportController
        : ControllerBase
    {
        private readonly RawMaterialReportService _service;

        public RawMaterialReportController(
            RawMaterialReportService service)
        {
            _service = service;
        }

        [HttpGet("releases")]
        public async Task<IActionResult>
            GetReleaseReport(
                [FromQuery]
                RawMaterialReleaseReportFilterDto filter)
        {
            try
            {
                var result =
                    await _service
                        .GetReleaseReportAsync(filter);

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
                    });
            }
        }


        [HttpGet("aging")]
        public async Task<IActionResult> GetAgingReport(
    [FromQuery]
    RawMaterialAgingReportFilterDto filter)
        {
            try
            {
                var result =
                    await _service
                        .GetAgingReportAsync(filter);

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

        [HttpGet("usage-trend")]
        public async Task<IActionResult> GetUsageTrendReport(
    [FromQuery]
    RawMaterialUsageTrendFilterDto filter)
        {
            try
            {
                var result =
                    await _service
                        .GetUsageTrendReportAsync(filter);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Failed to load Raw Material Usage Trend report.",
                        error = ex.Message
                    }
                );
            }
        }

        [HttpGet("forecast")]
        public async Task<IActionResult> GetForecastReport(
    [FromQuery]
    RawMaterialForecastFilterDto filter)
        {
            try
            {
                var result =
                    await _service
                        .GetForecastReportAsync(filter);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Failed to load Raw Material Forecast / Reorder report.",
                        error = ex.Message
                    }
                );
            }
        }
    }
}