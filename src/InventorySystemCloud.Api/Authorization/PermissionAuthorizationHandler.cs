using System.Threading.Tasks;
using InventorySystemCloud.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace InventorySystemCloud.Api.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Administrator role bypasses all individual permission checks
            if (context.User.IsInRole("Admin") || context.User.IsInRole(UserRole.Admin.ToString()))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check if the user has the specific permission claim
            if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
