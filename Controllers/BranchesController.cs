using Google.Api;
using inventory_api.DTOs;
using inventory_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchesController : ControllerBase
    {
        private readonly BranchesService _branchesService;

        public BranchesController(BranchesService branchesService)
        {
            _branchesService = branchesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _branchesService.GetAllAsync();
            return Ok(result);
        }

       

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBranchesDto dto)
        {
            await _branchesService.AddAsync(dto);
            return Ok(new { message = "Branches added successfully" });
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> Delete(string productId)
        {
            var result = await _branchesService.SoftDeleteAsync(productId);

            if (!result)
                return NotFound(new { message = "Branches not found" });

            return Ok(new { message = "Branches deleted successfully" });
        }

        [HttpDelete("reset")]
public async Task<IActionResult> ResetAll()
{
    await _branchesService.ResetAllAsync();
    return Ok(new { message = "All Branches deleted" });
}


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(string id, [FromBody] CreateBranchesDto dto)
        {
            try
            {
                await _branchesService.UpdateAsync(id, dto);
                return Ok(new { message = "Branch updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


    }
}