using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductLotNumberController : ControllerBase
    {
        private readonly ProductLotNumberService _productLotNumberService;

        public ProductLotNumberController(ProductLotNumberService productLotNumberService)
        {
            _productLotNumberService = productLotNumberService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _productLotNumberService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("LotNumber/{lotNo}")]
        public async Task<IActionResult> GetByLotNo(string lotNo)
        {
            var result = await _productLotNumberService.GetByLotNoAsync(lotNo);

            if (result == null)
                return NotFound(new { message = "Lot Number not found" });

            return Ok(result);
        }

        [HttpGet("ProductID/{productId}")]
        public async Task<IActionResult> GetByProductID(string productId)
        {
            var result = await _productLotNumberService.GetByProductIDAsync(productId);

            if (result == null || result.Count == 0)
                return NotFound(new { message = "Product ID not found" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductLotNumberDto dto)
        {
            await _productLotNumberService.AddAsync(dto);
            return Ok(new { message = "Lot No added successfully" });
        }

        //[HttpDelete("{productId}")]
        //public async Task<IActionResult> Delete(string productId)
        //{
        //    var result = await _productLotNumberService.SoftDeleteAsync(productId);

        //    if (!result)
        //        return NotFound(new { message = "Product not found" });

        //    return Ok(new { message = "Product deleted successfully" });
        //}

        [HttpDelete("reset")]
public async Task<IActionResult> ResetAll()
{
    await _productLotNumberService.ResetAllAsync();
    return Ok(new { message = "All Lot Number deleted" });
}


        [HttpPut("rename-lot")]
        public async Task<IActionResult> RenameLot([FromBody] RenameLotNumberDto dto)
        {
            try
            {
                await _productLotNumberService.RenameLotAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "Lot number updated successfully."
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

        [HttpGet("check-similar")]
        public async Task<IActionResult> CheckSimilarLot(
    string productId,
    string branchId,
    string lotNo)
        {
            try
            {
                var result = await _productLotNumberService
                    .CheckSimilarLotAsync(productId, branchId, lotNo);

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