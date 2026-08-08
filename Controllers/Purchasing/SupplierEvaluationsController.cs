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
    /// Generates one monthly supplier evaluation.
    /// The initial status is GENERATED.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<SupplierEvaluationResultDto>>
        Generate(
            [FromBody] GenerateSupplierEvaluationDto request)
    {
        var result =
            await _service.GenerateEvaluationAsync(request);

        if (!result.Success)
        {
            return Conflict(result);
        }

        return CreatedAtAction(
            nameof(GetDetails),
            new { id = result.EvaluationId },
            result);
    }

    /// <summary>
    /// Previews automatic supplier evaluation metrics without saving.
    /// </summary>
    [HttpGet("preview")]
    public async Task<ActionResult<SupplierEvaluationGeneratedMetrics>>
        Preview(
            int supplierId,
            int year,
            int month)
    {
        var result =
            await _service.PreviewEvaluationAsync(
                supplierId,
                year,
                month);

        return Ok(result);
    }

    /// <summary>
    /// Regenerates automatic metrics for a GENERATED evaluation.
    /// Manual reliability values are preserved.
    /// </summary>
    [HttpPost("{id:int}/regenerate")]
    public async Task<ActionResult<SupplierEvaluationResultDto>>
        Regenerate(
            int id,
            [FromBody] SupplierEvaluationWorkflowActionDto request)
    {
        var result =
            await _service.RegenerateAsync(id, request);

        return ToActionResult(result);
    }

    /// <summary>
    /// Finalizes a GENERATED supplier evaluation.
    /// </summary>
    [HttpPost("{id:int}/finalize")]
    public async Task<ActionResult<SupplierEvaluationResultDto>>
        FinalizeEvaluation(
            int id,
            [FromBody] SupplierEvaluationWorkflowActionDto request)
    {
        var result =
            await _service.FinalizeAsync(id, request);

        return ToActionResult(result);
    }

    /// <summary>
    /// Returns complete details of one evaluation.
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
                message = "Supplier evaluation was not found."
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Returns the supplier evaluation summary for one month.
    /// </summary>
    [HttpGet("summary/{year:int}/{month:int}")]
    public async Task<ActionResult<SupplierEvaluationMonthlySummaryDto>>
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