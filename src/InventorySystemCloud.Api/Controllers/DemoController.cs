using System;
using System.Threading.Tasks;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InventorySystemCloud.Api.Controllers
{
    /// <summary>
    /// Controller exclusively for the demo version.
    /// Provides test credentials, environment information, and a reset of the database state.
    /// </summary>
    [ApiController]
    [Route("demo")]
    public class DemoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DemoController> _logger;

        public DemoController(AppDbContext context, ILogger<DemoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtain the preconfigured credentials to test the application with different roles..
        /// </summary>
        [HttpGet("credentials")]
        public IActionResult GetCredentials()
        {
            return Ok(new
            {
                success = true,
                message = "Test credentials for the Demo environment.",
                data = new
                {
                    environment = "Demo",
                    accounts = new[]
                    {
                        new
                        {
                            role = "Administrador",
                            email = "admin@demo.com",
                            password = "AdminDemo123*!",
                            description = "Unrestricted access: User and cashier management, permission assignment, financial reports in Excel, auditing, products, customers, sales, and purchases."
                        },
                        new
                        {
                            role = "Cajero",
                            email = "cajero@demo.com",
                            password = "CashierDemo123*!",
                            description = "Operational access: Point of Sale (POS), sales generation, issuing and downloading invoices in PDF format, customer management, and catalog browsing."
                        }
                    },
                    features = new[]
                    {
                      "Point of Sale (POS) with automatic change calculation",
                      "Issuance of commercial invoices generated as PDFs using QuestPDF",
                      "Real-time inventory control and detection of out-of-stock products",
                      "Analytical sales reports by date range and top-selling products",
                      "Export of inventory and sales data to Excel (.xlsx) format",
                      "JWT security with claims and granular RBAC, including refresh tokens"
                   },
                    note = "In this demo environment, main users are protected against deactivation or deletion."
                }
            });
        }

        /// <summary>
        /// Obtain general information and status of the demo environment.
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetDemoInfo() => GetCredentials();

        /// <summary>
        ///Reset the demo database to its clean, initial state, including sample products, customers, sales, and purchases.
        /// </summary>
        [HttpPost("reset")]
        public async Task<IActionResult> ResetDemoDatabase()
        {
            try
            {
                await DemoDataSeeder.ResetDemoDataAsync(_context, _logger);
                return Ok(new
                {
                    success = true,
                    message = "The demonstration database has been successfully restored to its initial state.",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting the demo database.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while resetting the demo database. Please check the server logs."
                });
            }
        }
    }
}
