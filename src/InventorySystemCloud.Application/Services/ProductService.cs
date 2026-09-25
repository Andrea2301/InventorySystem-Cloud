using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Products;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Settings;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventorySystemCloud.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IAppDbContext _context;
        private readonly IImageStorageService _imageStorageService;
        private readonly CloudinarySettings _cloudinarySettings;

        public ProductService(
            IAppDbContext context,
            IImageStorageService imageStorageService,
            IOptions<CloudinarySettings> cloudinarySettings)
        {
            _context = context;
            _imageStorageService = imageStorageService;
            _cloudinarySettings = cloudinarySettings.Value;
        }

        public async Task<ApiResponse<IEnumerable<ProductResponseDto>>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.Products.AsQueryable();

            if (!includeInactive)
                query = query.Where(p => p.IsActive);

            var products = await query
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .Select(p => ToResponseDto(p))
                .ToListAsync();

            return ApiResponse<IEnumerable<ProductResponseDto>>.SuccessResponse(products);
        }

        public async Task<ApiResponse<ProductResponseDto>> GetByIdAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return ApiResponse<ProductResponseDto>.FailureResponse("Product not found.", statusCode: 404);

            return ApiResponse<ProductResponseDto>.SuccessResponse(ToResponseDto(product));
        }

        public async Task<ApiResponse<ProductResponseDto>> CreateAsync(CreateProductDto request)
        {
            var nameExists = await _context.Products
                .AnyAsync(p => p.Name.ToLower() == request.Name.Trim().ToLower());

            if (nameExists)
                return ApiResponse<ProductResponseDto>.FailureResponse("A product with that name already exists.", statusCode: 409);

            var product = new Product
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Category = request.Category.Trim(),
                Price = request.Price,
                Quantity = request.Quantity,
                ImagePath = request.ImagePath?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return ApiResponse<ProductResponseDto>.SuccessResponse(ToResponseDto(product), "Product created successfully.", statusCode: 201);
        }

        public async Task<ApiResponse<ProductResponseDto>> UpdateAsync(int id, UpdateProductDto request)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return ApiResponse<ProductResponseDto>.FailureResponse("Product not found.", statusCode: 404);

            var nameConflict = await _context.Products
                .AnyAsync(p => p.Name.ToLower() == request.Name.Trim().ToLower() && p.Id != id);

            if (nameConflict)
                return ApiResponse<ProductResponseDto>.FailureResponse("A product with that name already exists.", statusCode: 409);

            product.Name = request.Name.Trim();
            product.Description = request.Description?.Trim();
            product.Category = request.Category.Trim();
            product.Price = request.Price;
            product.Quantity = request.Quantity;
            product.ImagePath = request.ImagePath?.Trim();
            product.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return ApiResponse<ProductResponseDto>.SuccessResponse(ToResponseDto(product), "Product updated successfully.");
        }

        public async Task<ApiResponse<string>> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return ApiResponse<string>.FailureResponse("Product not found.", statusCode: 404);

            // Soft delete — preserve referential integrity with SaleDetails
            product.IsActive = false;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Product deactivated.", "Product deleted successfully.");
        }

        public async Task<ApiResponse<ProductResponseDto>> UploadImageAsync(int id, Stream fileStream, string fileName)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return ApiResponse<ProductResponseDto>.FailureResponse("Product not found.", statusCode: 404);

            // If an image already exists in Cloudinary, delete it first
            if (!string.IsNullOrEmpty(product.ImagePublicId))
            {
                await _imageStorageService.DeleteImageAsync(product.ImagePublicId);
            }

            var folder = !string.IsNullOrEmpty(_cloudinarySettings.ProductFolder)
                ? _cloudinarySettings.ProductFolder
                : "inventory/products";

            var uploadResult = await _imageStorageService.UploadImageAsync(fileStream, fileName, folder);

            if (!uploadResult.Success)
            {
                return ApiResponse<ProductResponseDto>.FailureResponse(
                    uploadResult.ErrorMessage ?? "Error uploading image to Cloudinary.",
                    statusCode: 500);
            }

            product.ImageUrl = uploadResult.Url;
            product.ImagePublicId = uploadResult.PublicId;
            product.ImagePath = uploadResult.Url;

            await _context.SaveChangesAsync();

            return ApiResponse<ProductResponseDto>.SuccessResponse(
                ToResponseDto(product),
                "Product image uploaded successfully.");
        }

        public async Task<ApiResponse<ProductResponseDto>> DeleteImageAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return ApiResponse<ProductResponseDto>.FailureResponse("Product not found.", statusCode: 404);

            if (!string.IsNullOrEmpty(product.ImagePublicId))
            {
                await _imageStorageService.DeleteImageAsync(product.ImagePublicId);
            }

            product.ImageUrl = null;
            product.ImagePublicId = null;
            product.ImagePath = null;

            await _context.SaveChangesAsync();

            return ApiResponse<ProductResponseDto>.SuccessResponse(
                ToResponseDto(product),
                "Product image deleted successfully.");
        }

        private static ProductResponseDto ToResponseDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Category = p.Category,
            Price = p.Price,
            Quantity = p.Quantity,
            IsActive = p.IsActive,
            Status = p.Status,
            ImagePath = p.ImagePath,
            ImageUrl = p.ImageUrl,
            ImagePublicId = p.ImagePublicId,
            CreatedAt = p.CreatedAt
        };
    }
}
