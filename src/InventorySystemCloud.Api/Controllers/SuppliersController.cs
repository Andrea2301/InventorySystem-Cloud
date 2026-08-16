using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Suppliers;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("suppliers")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, [FromQuery] string? search = null)
        {
            if (includeInactive && !User.IsInRole("Admin"))
                includeInactive = false;

            var result = await _supplierService.GetAllAsync(includeInactive, search);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _supplierService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto request)
        {
            var result = await _supplierService.CreateAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierDto request)
        {
            var result = await _supplierService.UpdateAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _supplierService.DeleteAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
