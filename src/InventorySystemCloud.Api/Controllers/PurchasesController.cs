using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Purchases;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("purchases")]
    [Authorize]
    public class PurchasesController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;

        public PurchasesController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseDto request)
        {
            var publicIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(publicIdClaim, out var userPublicId))
                return Unauthorized();

            var result = await _purchaseService.CreatePurchaseAsync(request, userPublicId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? supplierId = null)
        {
            var result = await _purchaseService.GetAllAsync(startDate, endDate, supplierId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _purchaseService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("reports/daily")]
        public async Task<IActionResult> GetDailyReport([FromQuery] DateTime? date = null)
        {
            var result = await _purchaseService.GetDailyReportAsync(date);
            return StatusCode(result.StatusCode, result);
        }
    }
}
