using System.Threading.Tasks;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("audit")]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 50)
        {
            var result = await _auditService.GetRecentLogsAsync(count);
            return StatusCode(result.StatusCode, result);
        }
    }
}
