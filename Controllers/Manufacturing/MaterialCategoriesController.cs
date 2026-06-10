using inventory_api.DTOs.Manufacturing.Materials;
using inventory_api.Services.Manufacturing.Materials;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Manufacturing
{
    [ApiController]
    [Route("api/manufacturing/material-categories")]
    public class MaterialCategoriesController : ControllerBase
    {
        private readonly MaterialCategoryService _service;

        public MaterialCategoriesController(MaterialCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Material category not found." });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMaterialCategoryDto dto)
        {
            try
            {
                await _service.AddAsync(dto);
                return Ok(new { message = "Material category added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateMaterialCategoryDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Material category updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.SoftDeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Material category not found." });

            return Ok(new { message = "Material category deleted successfully." });
        }
    }
}