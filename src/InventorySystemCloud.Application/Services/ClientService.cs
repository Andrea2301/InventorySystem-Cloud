using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Clients;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly IAppDbContext _context;

        public ClientService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<IEnumerable<ClientResponseDto>>> GetAllAsync(bool includeInactive = false, string? searchTerm = null)
        {
            var query = _context.Clients.AsQueryable();

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.DocumentNumber.ToLower().Contains(term) ||
                    c.FirstName.ToLower().Contains(term) ||
                    c.LastName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term));
            }

            var clients = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => ToResponseDto(c))
                .ToListAsync();

            return ApiResponse<IEnumerable<ClientResponseDto>>.SuccessResponse(clients);
        }

        public async Task<ApiResponse<ClientResponseDto>> GetByIdAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return ApiResponse<ClientResponseDto>.FailureResponse("Cliente no encontrado.", statusCode: 404);

            return ApiResponse<ClientResponseDto>.SuccessResponse(ToResponseDto(client));
        }

        public async Task<ApiResponse<ClientResponseDto>> GetByDocumentNumberAsync(string documentNumber)
        {
            if (string.IsNullOrWhiteSpace(documentNumber))
                return ApiResponse<ClientResponseDto>.FailureResponse("El número de documento es obligatorio.", statusCode: 400);

            var doc = documentNumber.Trim();
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.DocumentNumber == doc);
            if (client == null)
                return ApiResponse<ClientResponseDto>.FailureResponse("Cliente no encontrado.", statusCode: 404);

            return ApiResponse<ClientResponseDto>.SuccessResponse(ToResponseDto(client));
        }

        public async Task<ApiResponse<ClientResponseDto>> CreateAsync(CreateClientDto request)
        {
            var doc = request.DocumentNumber.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            var docExists = await _context.Clients.AnyAsync(c => c.DocumentNumber == doc);
            if (docExists)
                return ApiResponse<ClientResponseDto>.FailureResponse("Ya existe un cliente con ese número de documento.", statusCode: 409);

            var emailExists = await _context.Clients.AnyAsync(c => c.Email == email);
            if (emailExists)
                return ApiResponse<ClientResponseDto>.FailureResponse("Ya existe un cliente con ese correo electrónico.", statusCode: 409);

            var client = new Client
            {
                DocumentNumber = doc,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Address = request.Address?.Trim(),
                DateOfBirth = request.DateOfBirth,
                Email = email,
                PhoneNumber = request.PhoneNumber.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return ApiResponse<ClientResponseDto>.SuccessResponse(ToResponseDto(client), "Cliente creado exitosamente.", statusCode: 201);
        }

        public async Task<ApiResponse<ClientResponseDto>> UpdateAsync(int id, UpdateClientDto request)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return ApiResponse<ClientResponseDto>.FailureResponse("Cliente no encontrado.", statusCode: 404);

            var doc = request.DocumentNumber.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            var docConflict = await _context.Clients.AnyAsync(c => c.DocumentNumber == doc && c.Id != id);
            if (docConflict)
                return ApiResponse<ClientResponseDto>.FailureResponse("Ya existe otro cliente con ese número de documento.", statusCode: 409);

            var emailConflict = await _context.Clients.AnyAsync(c => c.Email == email && c.Id != id);
            if (emailConflict)
                return ApiResponse<ClientResponseDto>.FailureResponse("Ya existe otro cliente con ese correo electrónico.", statusCode: 409);

            client.DocumentNumber = doc;
            client.FirstName = request.FirstName.Trim();
            client.LastName = request.LastName.Trim();
            client.Address = request.Address?.Trim();
            client.DateOfBirth = request.DateOfBirth;
            client.Email = email;
            client.PhoneNumber = request.PhoneNumber.Trim();
            client.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return ApiResponse<ClientResponseDto>.SuccessResponse(ToResponseDto(client), "Cliente actualizado exitosamente.");
        }

        public async Task<ApiResponse<string>> DeleteAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return ApiResponse<string>.FailureResponse("Cliente no encontrado.", statusCode: 404);

            client.IsActive = false;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Cliente desactivado.", "Cliente eliminado exitosamente.");
        }

        private static ClientResponseDto ToResponseDto(Client c) => new()
        {
            Id = c.Id,
            DocumentNumber = c.DocumentNumber,
            FirstName = c.FirstName,
            LastName = c.LastName,
            FullName = c.FullName,
            Address = c.Address,
            DateOfBirth = c.DateOfBirth,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        };
    }
}
