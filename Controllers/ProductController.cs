using Google.Api;
using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _productService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("barcode/{barcode}")]
        public async Task<IActionResult> GetByBarcode(string barcode)
        {
            var result = await _productService.GetByBarcodeAsync(barcode);

            if (result == null)
                return NotFound(new { message = "Product not found" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            await _productService.AddAsync(dto);
            return Ok(new { message = "Product added successfully" });
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> Delete(string productId)
        {
            var result = await _productService.SoftDeleteAsync(productId);

            if (!result)
                return NotFound(new { message = "Product not found" });

            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetProductsLookup([FromQuery] string? categoryId, [FromQuery] string? search)
        {
            var result = await _productService.GetProductsLookupAsync(categoryId, search);
            return Ok(result);
        }


        [HttpDelete("reset")]
public async Task<IActionResult> ResetAll()
{
    await _productService.ResetAllAsync();
    return Ok(new { message = "All products deleted" });
}


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateProductDto dto)
        {
            try
            {
                await _productService.UpdateAsync(id, dto);
                return Ok(new { message = "Product updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



    }




}