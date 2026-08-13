using inventory_api.DTOs.Purchasing.SupplierEvaluations;
using inventory_api.Services.Purchasing.SupplierEvaluations;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing;

[ApiController]
[Route("api/purchasing/supplier-evaluations")]
public class SupplierEvaluationsController : ControllerBase
{
    private readonly SupplierEvaluationService _service;

    public SupplierEvaluationsController(
        SupplierEvaluationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns supplier evaluations.
    /// Evaluations are automatically generated after QC commit.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SupplierEvaluationListDto>>>
        GetAll(
            [FromQuery] SupplierEvaluationFilterDto filter)
    {
        var result =
            await _service.GetAllAsync(filter);

        return Ok(result);
    }

    /// <summary>
    /// Returns complete details of one supplier evaluation.
    /// Includes per-material evaluation lines.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierEvaluationDetailsDto>>
        GetDetails(int id)
    {
        var result =
            await _service.GetDetailsAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "Supplier evaluation was not found."
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Purchasing enters the manual Reliability / After Sales
    /// assessment for each evaluation line.
    ///
    /// COA / Documents = max 5
    /// Terms           = max 10
    /// Others          = max 5
    /// </summary>
    [HttpPut("{id:int}/reliability")]
    public async Task<ActionResult<SupplierEvaluationResultDto>>
        SaveReliability(
            int id,
            [FromBody]
            SaveSupplierEvaluationReliabilityDto request)
    {
        var result =
            await _service.SaveReliabilityAsync(
                id,
                request);

        return ToActionResult(result);
    }

    /// <summary>
    /// Finalizes a PENDING_PURCHASING evaluation.
    /// </summary>
    [HttpPost("{id:int}/finalize")]
    public async Task<ActionResult<SupplierEvaluationResultDto>>
        FinalizeEvaluation(
            int id,
            [FromBody]
            SupplierEvaluationWorkflowActionDto request)
    {
        var result =
            await _service.FinalizeAsync(
                id,
                request);

        return ToActionResult(result);
    }

    /// <summary>
    /// Monthly supplier performance report.
    ///
    /// Evaluations themselves are no longer generated monthly.
    /// This endpoint only groups delivery evaluations for reporting.
    /// </summary>
    [HttpGet("summary/{year:int}/{month:int}")]
    public async Task<
        ActionResult<SupplierEvaluationMonthlySummaryDto>>
        GetMonthlySummary(
            int year,
            int month)
    {
        var result =
            await _service.GetMonthlySummaryAsync(
                year,
                month);

        return Ok(result);
    }

    private ActionResult<SupplierEvaluationResultDto>
        ToActionResult(
            SupplierEvaluationResultDto result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        if (result.EvaluationId == null)
        {
            return NotFound(result);
        }

        return BadRequest(result);
    }
}