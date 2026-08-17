using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("sales")]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSaleDto request)
        {
            var publicIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(publicIdClaim, out var userPublicId))
                return Unauthorized();

            var result = await _saleService.CreateSaleAsync(request, userPublicId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? clientId = null)
        {
            var result = await _saleService.GetAllAsync(startDate, endDate, clientId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _saleService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}/invoice")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            var result = await _saleService.GetInvoicePdfAsync(id);
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            return File(result.Data, "application/pdf", $"Factura_Venta_{id:D6}.pdf");
        }

        [HttpPost("{id:int}/send-invoice")]
        public async Task<IActionResult> SendInvoice(int id)
        {
            var result = await _saleService.SendInvoiceEmailAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("reports/daily")]
        public async Task<IActionResult> GetDailyReport([FromQuery] DateTime? date = null)
        {
            var result = await _saleService.GetDailyReportAsync(date);
            return StatusCode(result.StatusCode, result);
        }
    }
}
