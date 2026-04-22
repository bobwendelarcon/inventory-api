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
    }
}