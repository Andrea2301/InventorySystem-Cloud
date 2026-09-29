using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Users;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Obtiene todos los usuarios registrados y sus permisos asignados.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _userService.GetAllAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Obtiene un usuario específico por su ID con sus permisos.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _userService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Registra un nuevo cajero con sus permisos iniciales por módulo.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCashier([FromBody] CreateCashierDto request)
        {
            var result = await _userService.CreateCashierAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Actualiza la lista de permisos asignados a un usuario/cajero.
        /// </summary>
        [HttpPut("{id:int}/permissions")]
        public async Task<IActionResult> UpdatePermissions(int id, [FromBody] UpdateUserPermissionsDto request)
        {
            var result = await _userService.UpdatePermissionsAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Activa o desactiva la cuenta de un usuario.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromQuery] bool isActive)
        {
            var result = await _userService.ToggleStatusAsync(id, isActive);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Retorna el catálogo completo de permisos del sistema agrupados por módulo.
        /// </summary>
        [HttpGet("catalog")]
        public IActionResult GetPermissionsCatalog()
        {
            var result = _userService.GetPermissionsCatalog();
            return StatusCode(result.StatusCode, result);
        }
    }
}
