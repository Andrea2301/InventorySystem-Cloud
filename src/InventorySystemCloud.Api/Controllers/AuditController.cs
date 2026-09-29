using System.Threading.Tasks;
using InventorySystemCloud.Api.Authorization;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystemCloud.Api.Controllers
{
    [ApiController]
    [Route("audit")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        [HasPermission(AppPermissions.Reports.Audit)]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 50)
        {
            var result = await _auditService.GetRecentLogsAsync(count);
            return StatusCode(result.StatusCode, result);
        }
    }
}
