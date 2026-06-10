using inventory_api.DTOs.Purchasing.Suppliers;
using inventory_api.Services.Purchasing.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers.Purchasing.Suppliers
{
    [ApiController]
    [Route("api/purchasing/suppliers")]
    public class SupplierController : ControllerBase
    {
        private readonly SupplierService _supplierService;

        public SupplierController(SupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            string? search,
            string? status,
            string? supplierType,
            int page = 1,
            int pageSize = 10)
        {
            var result = await _supplierService.GetPagedAsync(
                search,
                status,
                supplierType,
                page,
                pageSize);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);

            if (supplier == null)
                return NotFound();

            return Ok(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSupplierDto dto)
        {
            var id = await _supplierService.CreateAsync(dto);

            return Ok(new
            {
                success = true,
                supplierId = id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateSupplierDto dto)
        {
            var success = await _supplierService.UpdateAsync(id, dto);

            if (!success)
                return NotFound();

            return Ok(new
            {
                success = true
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _supplierService.DeleteAsync(id);

            if (!success)
                return NotFound();

            return Ok(new
            {
                success = true
            });
        }
    }
}