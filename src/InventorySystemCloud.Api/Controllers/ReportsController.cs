using System;
using System.Threading.Tasks;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("reports")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Genera y descarga el reporte de productos e inventario en formato Excel (.xlsx).
        /// </summary>
        [HttpGet("products/excel")]
        public async Task<IActionResult> ExportProductsExcel()
        {
            var fileBytes = await _reportService.GenerateProductsExcelReportAsync();
            var fileName = $"Reporte_Inventario_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Genera y descarga el reporte de productos e inventario en formato CSV con soporte UTF-8.
        /// </summary>
        [HttpGet("products/csv")]
        public async Task<IActionResult> ExportProductsCsv()
        {
            var fileBytes = await _reportService.GenerateProductsCsvReportAsync();
            var fileName = $"Reporte_Inventario_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
            return File(fileBytes, "text/csv; charset=utf-8", fileName);
        }

        /// <summary>
        /// Genera y descarga el reporte consolidado de ventas en formato Excel (.xlsx).
        /// </summary>
        [HttpGet("sales/excel")]
        public async Task<IActionResult> ExportSalesExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var fileBytes = await _reportService.GenerateSalesReportAsync(startDate, endDate);
            var fileName = $"Reporte_Ventas_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Genera y descarga el reporte consolidado de compras en formato Excel (.xlsx).
        /// </summary>
        [HttpGet("purchases/excel")]
        public async Task<IActionResult> ExportPurchasesExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var fileBytes = await _reportService.GeneratePurchasesReportAsync(startDate, endDate);
            var fileName = $"Reporte_Compras_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Genera y descarga el reporte de logs de auditoría en formato Excel (.xlsx) - Solo Administradores.
        /// </summary>
        [HttpGet("audit/excel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportAuditExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var fileBytes = await _reportService.GenerateAuditLogsReportAsync(startDate, endDate);
            var fileName = $"Reporte_Auditoria_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
