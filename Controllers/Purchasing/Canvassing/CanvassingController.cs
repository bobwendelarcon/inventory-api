using inventory_api.DTOs.Purchasing.Canvassing;
using inventory_api.Services.Purchasing.Canvassing;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.Canvassing
{
    [ApiController]
    [Route("api/purchasing/canvassing")]
    public class CanvassingController : ControllerBase
    {
        private readonly CanvassingService _canvassingService;

        public CanvassingController(CanvassingService canvassingService)
        {
            _canvassingService = canvassingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            string? search,
            string? status,
            int page = 1,
            int pageSize = 10)
        {
            var result = await _canvassingService.GetPagedAsync(
                search,
                status,
                page,
                pageSize);

            return Ok(result);
        }

        [HttpPost("from-mprf/{mprfId}")]
        public async Task<IActionResult> CreateFromMprf(int mprfId)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value
                             ?? User.FindFirst("UserId")?.Value
                             ?? "SYSTEM";

                var canvassId = await _canvassingService.CreateFromMprfAsync(
                    mprfId,
                    userId);

                return Ok(new
                {
                    message = "Canvassing created successfully.",
                    canvassId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("{canvassId}")]
        public async Task<IActionResult> GetById(int canvassId)
        {
            var result = await _canvassingService.GetByIdAsync(canvassId);

            if (result == null)
                return NotFound(new { message = "Canvassing record not found." });

            return Ok(result);
        }

        [HttpPost("quotes")]
        public async Task<IActionResult> AddQuote([FromBody] CreateCanvassQuoteDto dto)
        {
            try
            {
                var quoteId = await _canvassingService.AddQuoteAsync(dto);

                return Ok(new
                {
                    message = "Supplier quote added successfully.",
                    quoteId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("{canvassId}/recommend")]
        public async Task<IActionResult> Recommend(int canvassId)
        {
            try
            {
                await _canvassingService.RecommendAsync(canvassId);

                return Ok(new
                {
                    message = "Supplier recommendation updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("{canvassId}/complete")]
        public async Task<IActionResult> Complete(int canvassId)
        {
            try
            {
                await _canvassingService.CompleteAsync(canvassId);

                return Ok(new
                {
                    message = "Canvassing completed successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("materials/{materialId}/suppliers")]
        public async Task<IActionResult> GetLinkedSuppliersByMaterial(int materialId)
        {
            var result = await _canvassingService.GetLinkedSuppliersByMaterialAsync(materialId);
            return Ok(result);
        }


        [HttpPut("quotes/{quoteId}")]
        public async Task<IActionResult> UpdateQuote(int quoteId, [FromBody] UpdateCanvassQuoteDto dto)
        {
            try
            {
                var result = await _canvassingService.UpdateQuoteAsync(quoteId, dto);

                if (!result)
                    return NotFound(new { message = "Quote not found." });

                return Ok(new { message = "Quote updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("quotes/{quoteId}")]
        public async Task<IActionResult> DeleteQuote(int quoteId)
        {
            try
            {
                var result = await _canvassingService.DeleteQuoteAsync(quoteId);

                if (!result)
                    return NotFound(new { message = "Quote not found." });

                return Ok(new { message = "Quote deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("quotes/{quoteId}/recommend")]
        public async Task<IActionResult> ManualRecommend(int quoteId)
        {
            try
            {
                await _canvassingService.ManualRecommendAsync(quoteId);

                return Ok(new
                {
                    message = "Supplier recommendation updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


    }
}