using inventory_api.DTOs.Purchasing.Suppliers.SupplierManufacturers;
using inventory_api.Services.Purchasing.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.Suppliers
{
    [ApiController]
    [Route("api/purchasing/supplier-manufacturers")]
    public class SupplierManufacturerController : ControllerBase
    {
        private readonly SupplierManufacturerService _service;

        public SupplierManufacturerController(SupplierManufacturerService service)
        {
            _service = service;
        }

        [HttpGet("supplier/{supplierId}")]
        public async Task<IActionResult> GetBySupplier(int supplierId)
        {
            var result = await _service.GetBySupplierAsync(supplierId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierManufacturerDto dto)
        {
            var id = await _service.CreateAsync(dto);

            return Ok(new
            {
                success = true,
                supplierManufacturerId = id
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);

            if (!success)
                return NotFound();

            return Ok(new
            {
                success = true
            });
        }
    }
}