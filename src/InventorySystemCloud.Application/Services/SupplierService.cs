using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Suppliers;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IAppDbContext _context;

        public SupplierService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<IEnumerable<SupplierResponseDto>>> GetAllAsync(bool includeInactive = false, string? searchTerm = null)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!includeInactive)
                query = query.Where(s => s.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s =>
                    s.CompanyName.ToLower().Contains(term) ||
                    s.Email.ToLower().Contains(term) ||
                    (s.Category != null && s.Category.ToLower().Contains(term)));
            }

            var suppliers = await query
                .OrderBy(s => s.CompanyName)
                .Select(s => ToResponseDto(s))
                .ToListAsync();

            return ApiResponse<IEnumerable<SupplierResponseDto>>.SuccessResponse(suppliers);
        }

        public async Task<ApiResponse<SupplierResponseDto>> GetByIdAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return ApiResponse<SupplierResponseDto>.FailureResponse("Proveedor no encontrado.", statusCode: 404);

            return ApiResponse<SupplierResponseDto>.SuccessResponse(ToResponseDto(supplier));
        }

        public async Task<ApiResponse<SupplierResponseDto>> CreateAsync(CreateSupplierDto request)
        {
            var company = request.CompanyName.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            var companyExists = await _context.Suppliers.AnyAsync(s => s.CompanyName.ToLower() == company.ToLower());
            if (companyExists)
                return ApiResponse<SupplierResponseDto>.FailureResponse("Ya existe un proveedor con ese nombre de empresa.", statusCode: 409);

            var emailExists = await _context.Suppliers.AnyAsync(s => s.Email == email);
            if (emailExists)
                return ApiResponse<SupplierResponseDto>.FailureResponse("Ya existe un proveedor con ese correo electrónico.", statusCode: 409);

            var supplier = new Supplier
            {
                CompanyName = company,
                Email = email,
                PhoneNumber = request.PhoneNumber.Trim(),
                Website = request.Website?.Trim(),
                Category = request.Category?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return ApiResponse<SupplierResponseDto>.SuccessResponse(ToResponseDto(supplier), "Proveedor creado exitosamente.", statusCode: 201);
        }

        public async Task<ApiResponse<SupplierResponseDto>> UpdateAsync(int id, UpdateSupplierDto request)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return ApiResponse<SupplierResponseDto>.FailureResponse("Proveedor no encontrado.", statusCode: 404);

            var company = request.CompanyName.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            var companyConflict = await _context.Suppliers.AnyAsync(s => s.CompanyName.ToLower() == company.ToLower() && s.Id != id);
            if (companyConflict)
                return ApiResponse<SupplierResponseDto>.FailureResponse("Ya existe otro proveedor con ese nombre de empresa.", statusCode: 409);

            var emailConflict = await _context.Suppliers.AnyAsync(s => s.Email == email && s.Id != id);
            if (emailConflict)
                return ApiResponse<SupplierResponseDto>.FailureResponse("Ya existe otro proveedor con ese correo electrónico.", statusCode: 409);

            supplier.CompanyName = company;
            supplier.Email = email;
            supplier.PhoneNumber = request.PhoneNumber.Trim();
            supplier.Website = request.Website?.Trim();
            supplier.Category = request.Category?.Trim();
            supplier.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return ApiResponse<SupplierResponseDto>.SuccessResponse(ToResponseDto(supplier), "Proveedor actualizado exitosamente.");
        }

        public async Task<ApiResponse<string>> DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return ApiResponse<string>.FailureResponse("Proveedor no encontrado.", statusCode: 404);

            supplier.IsActive = false;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Proveedor desactivado.", "Proveedor eliminado exitosamente.");
        }

        private static SupplierResponseDto ToResponseDto(Supplier s) => new()
        {
            Id = s.Id,
            CompanyName = s.CompanyName,
            Email = s.Email,
            PhoneNumber = s.PhoneNumber,
            Website = s.Website,
            Category = s.Category,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}
