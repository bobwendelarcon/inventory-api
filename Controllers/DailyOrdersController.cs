using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DailyOrdersController : ControllerBase
    {
        private readonly DailyOrderService _service;

        public DailyOrdersController(DailyOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? className,
            [FromQuery] int? year,
            [FromQuery] string? month,
            [FromQuery] string? status,
            [FromQuery] string? search)
        {
            var result = await _service.GetAllAsync(className, year, month, status, search);
            return Ok(result);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetById(long orderId)
        {
            try
            {
                var result = await _service.GetByIdAsync(orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDailyOrderRequest request)
        {
            var result = await _service.CreateAsync(request);
            return Ok(result);
        }

        [HttpPost("{orderId}/allocate")]
        public async Task<IActionResult> Allocate(long orderId)
        {
            var result = await _service.AllocateAsync(orderId);
            return Ok(result);
        }

        [HttpPost("{orderId}/ready-for-dispatch")]
        public async Task<IActionResult> MarkReadyForDispatch(long orderId)
        {
            try
            {
                var result = await _service.MarkReadyForDispatchAsync(orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> Update(long orderId, [FromBody] UpdateDailyOrderRequest request)
        {
            try
            {
                var result = await _service.UpdateHeaderAsync(orderId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{orderId}")]
        public async Task<IActionResult> Delete(long orderId)
        {
            try
            {
                var result = await _service.DeleteAsync(orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}