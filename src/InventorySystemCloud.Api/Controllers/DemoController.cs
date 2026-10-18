using System;
using System.Threading.Tasks;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InventorySystemCloud.Api.Controllers
{
    /// <summary>
    /// Controlador exclusivo para la versión de demostración (Demo).
    /// Provee credenciales de prueba, información del entorno y reinicio del estado de la base de datos.
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
        /// Obtiene las credenciales preconfiguradas para probar la aplicación con distintos roles.
        /// </summary>
        [HttpGet("credentials")]
        public IActionResult GetCredentials()
        {
            return Ok(new
            {
                success = true,
                message = "Credenciales del entorno Demo para pruebas funcionales.",
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
                            description = "Acceso irrestricto: Gestión de usuarios y cajeros, asignación de permisos, reportes financieros en Excel, auditoría, productos, clientes, ventas y compras."
                        },
                        new
                        {
                            role = "Cajero",
                            email = "cajero@demo.com",
                            password = "CashierDemo123*!",
                            description = "Acceso operativo: Punto de Venta (POS), generación de ventas, emisión y descarga de facturas en PDF, gestión de clientes y consulta de catálogo."
                        }
                    },
                    features = new[]
                    {
                        "Punto de Venta (POS) con cálculo automático de cambio",
                        "Emisión de facturas comerciales generadas en PDF con QuestPDF",
                        "Control de inventario en tiempo real y detección de productos sin stock",
                        "Reportes analíticos de ventas por rango de fechas y productos más vendidos",
                        "Exportación de inventario y ventas a formato Excel (.xlsx)",
                        "Seguridad JWT con Claims y RBAC granular con Refresh Tokens"
                    },
                    note = "En este entorno demo, los usuarios principales están protegidos contra desactivación o borrado."
                }
            });
        }

        /// <summary>
        /// Retorna información general y estado del entorno demo.
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetDemoInfo() => GetCredentials();

        /// <summary>
        /// Restablece la base de datos demo al estado inicial limpio con productos, clientes, ventas y compras de prueba.
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
                    message = "La base de datos de demostración ha sido restablecida exitosamente a su estado inicial.",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer la base de datos demo.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ocurrió un error al restablecer la base de datos demo. Consulte los registros del servidor."
                });
            }
        }
    }
}
