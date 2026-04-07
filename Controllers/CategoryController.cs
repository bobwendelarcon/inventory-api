using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoryController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _categoryService.GetAllAsync();
            return Ok(result);
        }

        //[HttpGet("barcode/{barcode}")]
        //public async Task<IActionResult> GetByBarcode(string barcode)
        //{
        //    var result = await _categoryService.GetByBarcodeAsync(barcode);

        //    if (result == null)
        //        return NotFound(new { message = "Product not found" });

        //    return Ok(result);
        //}

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            await _categoryService.AddAsync(dto);
            return Ok(new { message = "Category added successfully" });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateCategoryDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid request.");

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Category ID is required.");

            await _categoryService.UpdateAsync(id, dto);
            return Ok(new { message = "Category updated successfully." });
        }
    }
}