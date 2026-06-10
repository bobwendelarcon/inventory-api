using inventory_api.DTOs.Purchasing.Suppliers.Mappings;
using inventory_api.Services.Purchasing.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.Suppliers
{
    [ApiController]
    [Route("api/purchasing/supplier-materials")]
    public class SupplierMaterialController : ControllerBase
    {
        private readonly SupplierMaterialService _supplierMaterialService;

        public SupplierMaterialController(SupplierMaterialService supplierMaterialService)
        {
            _supplierMaterialService = supplierMaterialService;
        }

        [HttpGet("supplier/{supplierId}")]
        public async Task<IActionResult> GetBySupplier(int supplierId)
        {
            var result = await _supplierMaterialService.GetBySupplierAsync(supplierId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierMaterialDto dto)
        {
            var id = await _supplierMaterialService.CreateAsync(dto);

            return Ok(new
            {
                success = true,
                supplierMaterialId = id
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _supplierMaterialService.DeleteAsync(id);

            if (!success)
                return NotFound();

            return Ok(new
            {
                success = true
            });
        }
    }
}