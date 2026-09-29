using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Users;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Constants;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class UserService : IUserService
    {
        private const int PasswordWorkFactor = 12;
        private readonly IAppDbContext _context;
        private readonly IEmailService _emailService;

        public UserService(IAppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<ApiResponse<List<UserResponseDto>>> GetAllAsync()
        {
            var users = await _context.Users
                .Include(u => u.Permissions)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var dtos = users.Select(MapToDto).ToList();
            return ApiResponse<List<UserResponseDto>>.SuccessResponse(dtos, "Usuarios obtenidos exitosamente.");
        }

        public async Task<ApiResponse<UserResponseDto>> GetByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResponse<UserResponseDto>.FailureResponse("Usuario no encontrado.", statusCode: 404);

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToDto(user), "Usuario obtenido exitosamente.");
        }

        public async Task<ApiResponse<UserResponseDto>> CreateCashierAsync(CreateCashierDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            if (exists)
                return ApiResponse<UserResponseDto>.FailureResponse("Ya existe un usuario con este correo electrónico.", statusCode: 400);

            if (!IsPasswordStrong(request.Password))
                return ApiResponse<UserResponseDto>.FailureResponse("La contraseña debe tener al menos 8 caracteres e incluir mayúsculas, minúsculas, números y caracteres especiales.", statusCode: 400);

            var validPermissions = FilterValidPermissions(request.Permissions);

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: PasswordWorkFactor),
                Role = UserRole.Cashier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };

            foreach (var perm in validPermissions)
            {
                user.Permissions.Add(new UserPermission
                {
                    Permission = perm,
                    User = user
                });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send welcome notification asynchronously
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Email);
            }
            catch
            {
                // Non-blocking email error
            }

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToDto(user), "Cajero creado exitosamente.", statusCode: 201);
        }

        public async Task<ApiResponse<UserResponseDto>> UpdatePermissionsAsync(int id, UpdateUserPermissionsDto request)
        {
            var user = await _context.Users
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResponse<UserResponseDto>.FailureResponse("Usuario no encontrado.", statusCode: 404);

            if (user.Role == UserRole.Admin)
                return ApiResponse<UserResponseDto>.FailureResponse("No se pueden modificar los permisos individuales del Administrador.", statusCode: 400);

            var validPermissions = FilterValidPermissions(request.Permissions);

            // Remove existing permissions
            _context.UserPermissions.RemoveRange(user.Permissions);
            user.Permissions.Clear();

            // Add new permissions
            foreach (var perm in validPermissions)
            {
                user.Permissions.Add(new UserPermission
                {
                    UserId = user.Id,
                    Permission = perm
                });
            }

            // Invalidate existing sessions immediately so new permissions take effect
            user.SecurityStamp = Guid.NewGuid().ToString("N");

            await _context.SaveChangesAsync();
            return ApiResponse<UserResponseDto>.SuccessResponse(MapToDto(user), "Permisos actualizados exitosamente.");
        }

        public async Task<ApiResponse<UserResponseDto>> ToggleStatusAsync(int id, bool isActive)
        {
            var user = await _context.Users
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResponse<UserResponseDto>.FailureResponse("Usuario no encontrado.", statusCode: 404);

            if (user.Role == UserRole.Admin && !isActive)
                return ApiResponse<UserResponseDto>.FailureResponse("No se puede desactivar la cuenta del Administrador principal.", statusCode: 400);

            user.IsActive = isActive;
            // Invalidate existing active tokens
            user.SecurityStamp = Guid.NewGuid().ToString("N");

            await _context.SaveChangesAsync();
            var message = isActive ? "Usuario activado exitosamente." : "Usuario desactivado exitosamente.";
            return ApiResponse<UserResponseDto>.SuccessResponse(MapToDto(user), message);
        }

        public ApiResponse<List<PermissionGroupDto>> GetPermissionsCatalog()
        {
            var catalog = new List<PermissionGroupDto>
            {
                new PermissionGroupDto
                {
                    Module = "Productos",
                    Permissions = new List<PermissionItemDto>
                    {
                        new() { Key = AppPermissions.Products.View, Description = "Ver catálogo de productos" },
                        new() { Key = AppPermissions.Products.Create, Description = "Crear nuevos productos" },
                        new() { Key = AppPermissions.Products.Edit, Description = "Editar productos existentes" },
                        new() { Key = AppPermissions.Products.Delete, Description = "Eliminar productos" }
                    }
                },
                new PermissionGroupDto
                {
                    Module = "Clientes",
                    Permissions = new List<PermissionItemDto>
                    {
                        new() { Key = AppPermissions.Clients.View, Description = "Ver lista de clientes" },
                        new() { Key = AppPermissions.Clients.Create, Description = "Crear nuevos clientes" },
                        new() { Key = AppPermissions.Clients.Edit, Description = "Editar clientes existentes" },
                        new() { Key = AppPermissions.Clients.Delete, Description = "Eliminar clientes" }
                    }
                },
                new PermissionGroupDto
                {
                    Module = "Proveedores",
                    Permissions = new List<PermissionItemDto>
                    {
                        new() { Key = AppPermissions.Suppliers.View, Description = "Ver lista de proveedores" },
                        new() { Key = AppPermissions.Suppliers.Create, Description = "Crear proveedores" },
                        new() { Key = AppPermissions.Suppliers.Edit, Description = "Editar proveedores" },
                        new() { Key = AppPermissions.Suppliers.Delete, Description = "Eliminar proveedores" }
                    }
                },
                new PermissionGroupDto
                {
                    Module = "Ventas",
                    Permissions = new List<PermissionItemDto>
                    {
                        new() { Key = AppPermissions.Sales.View, Description = "Ver historial de ventas" },
                        new() { Key = AppPermissions.Sales.Create, Description = "Registrar nuevas ventas" },
                        new() { Key = AppPermissions.Sales.Invoice, Description = "Descargar y enviar facturas por correo" }
                    }
                },
                new PermissionGroupDto
                {
                    Module = "Compras",
                    Permissions = new List<PermissionItemDto>
                    {
                        new() { Key = AppPermissions.Purchases.View, Description = "Ver historial de compras" },
                        new() { Key = AppPermissions.Purchases.Create, Description = "Registrar órdenes de compra" }
                    }
                },
                new PermissionGroupDto
                {
                    Module = "Reportes",
                    Permissions = new List<PermissionItemDto>
                    {
                        new() { Key = AppPermissions.Reports.Products, Description = "Exportar reportes de productos" },
                        new() { Key = AppPermissions.Reports.Sales, Description = "Exportar reportes de ventas" },
                        new() { Key = AppPermissions.Reports.Purchases, Description = "Exportar reportes de compras" },
                        new() { Key = AppPermissions.Reports.Audit, Description = "Ver y exportar logs de auditoría" }
                    }
                },
                new PermissionGroupDto
                {
                    Module = "Usuarios",
                    Permissions = new List<PermissionItemDto>
                    {
                        new() { Key = AppPermissions.Users.View, Description = "Ver usuarios y cajeros" },
                        new() { Key = AppPermissions.Users.Manage, Description = "Crear cajeros y modificar permisos" }
                    }
                }
            };

            return ApiResponse<List<PermissionGroupDto>>.SuccessResponse(catalog, "Catálogo de permisos obtenido exitosamente.");
        }

        private static List<string> FilterValidPermissions(IEnumerable<string>? permissions)
        {
            if (permissions == null) return new List<string>();
            var validSet = new HashSet<string>(AppPermissions.All, StringComparer.OrdinalIgnoreCase);
            return permissions.Where(p => validSet.Contains(p)).Distinct().ToList();
        }

        private static UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                PublicId = user.PublicId,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                AvatarUrl = user.AvatarUrl,
                Permissions = user.Role == UserRole.Admin
                    ? AppPermissions.All.ToList()
                    : (user.Permissions?.Select(p => p.Permission).ToList() ?? new List<string>())
            };
        }

        private static bool IsPasswordStrong(string password) =>
            password.Length is >= 8 and <= 128 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsLower) &&
            password.Any(char.IsDigit) &&
            password.Any(c => !char.IsLetterOrDigit(c));
    }
}
