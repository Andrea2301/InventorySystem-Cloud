using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Products;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("products")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5MB

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

        /// <summary>
        /// Uploads an image to Cloudinary and links it to the product.
        /// </summary>
        [HttpPost("{id:int}/image")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Debe proporcionar un archivo de imagen válido." });

            if (file.Length > MaxFileSizeInBytes)
                return BadRequest(new { success = false, message = "El tamaño máximo permitido para la imagen es de 5 MB." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "Formato de archivo no soportado. Formatos válidos: JPG, PNG, WEBP." });

            await using var stream = file.OpenReadStream();
            var result = await _productService.UploadImageAsync(id, stream, file.FileName);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Deletes the product image from Cloudinary and removes the link from the product.
        /// </summary>
        [HttpDelete("{id:int}/image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var result = await _productService.DeleteImageAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
