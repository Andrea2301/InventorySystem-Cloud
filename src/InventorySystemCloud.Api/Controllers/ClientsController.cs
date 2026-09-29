using System.Threading.Tasks;
using InventorySystemCloud.Api.Authorization;
using InventorySystemCloud.Application.DTOs.Clients;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("clients")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        [HasPermission(AppPermissions.Clients.View)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, [FromQuery] string? search = null)
        {
            if (includeInactive && !User.IsInRole("Admin"))
                includeInactive = false;

            var result = await _clientService.GetAllAsync(includeInactive, search);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        [HasPermission(AppPermissions.Clients.View)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _clientService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("by-document/{documentNumber}")]
        [HasPermission(AppPermissions.Clients.View)]
        public async Task<IActionResult> GetByDocumentNumber(string documentNumber)
        {
            var result = await _clientService.GetByDocumentNumberAsync(documentNumber);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [HasPermission(AppPermissions.Clients.Create)]
        public async Task<IActionResult> Create([FromBody] CreateClientDto request)
        {
            var result = await _clientService.CreateAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        [HasPermission(AppPermissions.Clients.Edit)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDto request)
        {
            var result = await _clientService.UpdateAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        [HasPermission(AppPermissions.Clients.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _clientService.DeleteAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
