using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAppDbContext _context;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthService(IAppDbContext context, IJwtTokenGenerator tokenGenerator)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("El correo electrónico y la contraseña son obligatorios.", statusCode: 400);
            }

            if (request.Password.Length < 6)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("La contraseña no cumple con los requisitos mínimos (mínimo 6 caracteres).", statusCode: 400);
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (existingUser)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Ya existe un usuario registrado con este correo electrónico.", statusCode: 400);
            }

            var user = new User
            {
                Email = request.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
                Role = UserRole.Cashier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _tokenGenerator.GenerateToken(user);
            var response = new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Registro exitoso.", statusCode: 201);
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Correo electrónico o contraseña inválidos.", statusCode: 401);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("Credenciales incorrectas.", statusCode: 401);
            }

            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.FailureResponse("La cuenta de usuario se encuentra inactiva.", statusCode: 403);
            }

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _tokenGenerator.GenerateToken(user);
            var response = new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Inicio de sesión exitoso.", statusCode: 200);
        }
    }
}
