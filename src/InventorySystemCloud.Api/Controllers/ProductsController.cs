using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Products;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("products")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Returns all active products. Admins can also request inactive ones.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            // Only Admins can see inactive products
            if (includeInactive && !User.IsInRole("Admin"))
                includeInactive = false;

            var result = await _productService.GetAllAsync(includeInactive);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto request)
        {
            var result = await _productService.CreateAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto request)
        {
            var result = await _productService.UpdateAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
