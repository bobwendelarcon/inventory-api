using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryChecklistController : ControllerBase
    {
        private readonly DeliveryChecklistService _deliveryChecklistService;

        public DeliveryChecklistController(DeliveryChecklistService deliveryChecklistService)
        {
            _deliveryChecklistService = deliveryChecklistService;
        }


        [HttpPost("create")]
        public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistDto dto)
        {
            try
            {
                var createdBy = User.Identity?.Name ?? "admin";
                var result = await _deliveryChecklistService.CreateChecklistAsync(dto, createdBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("ready-for-checklist")]
        public async Task<IActionResult> GetReadyForChecklist()
        {
            try
            {
                var result = await _deliveryChecklistService.GetReadyForChecklistAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetChecklistList(
    DateTime? date,
    string? status,
    string? truck,
    string? search)
        {
            try
            {
                var data = await _deliveryChecklistService.GetChecklistListAsync(date, status, truck, search);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetChecklistDetails(long id)
        {
            try
            {
                var result = await _deliveryChecklistService.GetChecklistDetailsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("delete/{id:long}")]
        public async Task<IActionResult> DeleteChecklist(long id)
        {
            try
            {
                await _deliveryChecklistService.DeleteChecklistAsync(id);

                return Ok(new
                {
                    success = true,
                    message = "Checklist deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //confirm loading delivery checklist

        [HttpPost("confirm-loading/{id:long}")]
        public async Task<IActionResult> ConfirmLoading(long id)
        {
            try
            {
                await _deliveryChecklistService.ConfirmLoadingAsync(id);

                return Ok(new
                {
                    success = true,
                    message = "Checklist is now LOADING."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("complete-line")]
        public async Task<IActionResult> CompleteLine([FromBody] CompleteChecklistLineDto dto)
        {
            try
            {
                var completedBy =
                    !string.IsNullOrWhiteSpace(dto.adjusted_by)
                        ? dto.adjusted_by
                        : User.Identity?.Name ?? "admin";

                var result = await _deliveryChecklistService.CompleteLineAsync(dto, completedBy);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.ToString()
                });
            }
        }

        [HttpPost("update-line-lot")]
        public async Task<IActionResult> UpdateChecklistLineLot([FromBody] UpdateChecklistLineLotDto dto)
        {
            try
            {
                var result = await _deliveryChecklistService.UpdateChecklistLineLotAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("available-lots-for-line/{checklistLineId:long}")]
        public async Task<IActionResult> GetAvailableLotsForChecklistLine(long checklistLineId)
        {
            try
            {
                var result = await _deliveryChecklistService.GetAvailableLotsForChecklistLineAsync(checklistLineId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.ToString()
                });
            }
        }

        [HttpPost("replace-checklist-lots")]
        public async Task<IActionResult> ReplaceChecklistLots([FromBody] ReplaceChecklistLotsDto dto)
        {
            try
            {
                var result = await _deliveryChecklistService.ReplaceChecklistLotsAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("complete-lines")]
        public async Task<IActionResult> CompleteLines(
    [FromBody] CompleteChecklistLinesDto dto)
        {
            try
            {
                var completedBy =
                    !string.IsNullOrWhiteSpace(dto.adjusted_by)
                        ? dto.adjusted_by
                        : User.Identity?.Name ?? "admin";

                var result = await _deliveryChecklistService
                    .CompleteLinesAsync(dto, completedBy);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("complete-customer")]
        public async Task<IActionResult> CompleteCustomer(
    [FromBody] CompleteChecklistCustomerDto dto)
        {
            try
            {
                var completedBy =
                    !string.IsNullOrWhiteSpace(dto.adjusted_by)
                        ? dto.adjusted_by
                        : User.Identity?.Name ?? "admin";

                var result = await _deliveryChecklistService
                    .CompleteCustomerAsync(dto, completedBy);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("delete-line/{checklistLineId:long}")]
        public async Task<IActionResult> DeleteChecklistLine(long checklistLineId)
        {
            try
            {
                var result = await _deliveryChecklistService.DeleteChecklistLineAsync(checklistLineId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet("{checklistId:long}/available-lines")]
        public async Task<IActionResult> GetAvailableLinesForChecklist(
    long checklistId)
        {
            try
            {
                if (checklistId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid checklist ID."
                    });
                }

                var result = await _deliveryChecklistService
                    .GetAvailableLinesForChecklistAsync(checklistId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPost("add-lines")]
        public async Task<IActionResult> AddLinesToChecklist(
    [FromBody] AddChecklistLinesDto dto)
        {
            try
            {
                var result = await _deliveryChecklistService
                    .AddLinesToChecklistAsync(dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("remove-customer")]
        public async Task<IActionResult> RemoveCustomerFromChecklist(
    [FromBody] RemoveChecklistCustomerDto dto)
        {
            try
            {
                var result = await _deliveryChecklistService
                    .RemoveCustomerFromChecklistAsync(dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("update-trip-info")]
        public async Task<IActionResult> UpdateTripInfo(
    [FromBody] UpdateChecklistTripInfoDto dto)
        {
            try
            {
                var result = await _deliveryChecklistService
                    .UpdateTripInfoAsync(dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}