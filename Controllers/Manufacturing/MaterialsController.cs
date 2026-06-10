using inventory_api.DTOs.Manufacturing.Materials;
using inventory_api.Services.Manufacturing.Materials;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Manufacturing
{
    [ApiController]
    [Route("api/manufacturing/materials")]
    public class MaterialsController : ControllerBase
    {
        private readonly MaterialService _service;

        public MaterialsController(MaterialService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 50,
       [FromQuery] string? search = null,
       [FromQuery] int? categoryId = null,
       [FromQuery] int? subCategoryId = null,
       [FromQuery] bool? status = null)
        {
            var result = await _service.GetAllAsync(
                page,
                pageSize,
                search,
                categoryId,
                subCategoryId,
                status
            );

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Material not found." });

            return Ok(result);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup(
     [FromQuery] int? categoryId = null,
     [FromQuery] int? subCategoryId = null,
     [FromQuery] string? search = null)
        {
            var result = await _service.GetLookupAsync(
                categoryId,
                subCategoryId,
                search
            );

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMaterialDto dto)
        {
            try
            {
                await _service.AddAsync(dto);
                return Ok(new { message = "Material added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateMaterialDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Material updated successfully." });
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
                return NotFound(new { message = "Material not found." });

            return Ok(new { message = "Material deleted successfully." });
        }
    }
}