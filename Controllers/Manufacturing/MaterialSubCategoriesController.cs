using inventory_api.DTOs.Manufacturing.Materials;
using inventory_api.Services.Manufacturing.Materials;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Manufacturing
{
    [ApiController]
    [Route("api/manufacturing/material-subcategories")]
    public class MaterialSubCategoriesController : ControllerBase
    {
        private readonly MaterialSubCategoryService _service;

        public MaterialSubCategoriesController(
            MaterialSubCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            return Ok(await _service.GetByCategoryAsync(categoryId));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateMaterialSubCategoryDto dto)
        {
            await _service.AddAsync(dto);

            return Ok(new
            {
                message = "Sub category added successfully."
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            CreateMaterialSubCategoryDto dto)
        {
            await _service.UpdateAsync(id, dto);

            return Ok(new
            {
                message = "Sub category updated successfully."
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.SoftDeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                message = "Sub category deleted successfully."
            });
        }
    }
}