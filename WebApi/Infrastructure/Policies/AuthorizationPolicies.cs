using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebApi.Infrastructure.Policies
{
    // Requisito 1: ser Admin o AdminBranch (para web staff)
    public sealed class AdminOrBranchAdminRequirement : IAuthorizationRequirement { }

    public sealed class AdminOrBranchAdminHandler : AuthorizationHandler<AdminOrBranchAdminRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOrBranchAdminRequirement requirement)
        {
            var role = context.User.FindFirst("role")?.Value ?? "Staff";
            if (role is "Admin" or "AdminBranch") context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    // Requisito 2: tener un permiso concreto
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission) => Permission = permission;
    }

    public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.FindAll("perm").Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    // Requisito 3: alcance por sucursal (header X-Branch-Id debe coincidir salvo Admin)
    public sealed class BranchScopeRequirement : IAuthorizationRequirement { }

    public sealed class BranchScopeHandler : AuthorizationHandler<BranchScopeRequirement>
    {
        private readonly IHttpContextAccessor _http;

        public BranchScopeHandler(IHttpContextAccessor http) => _http = http;

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BranchScopeRequirement requirement)
        {
            var role = context.User.FindFirst("role")?.Value ?? "Staff";
            if (role == "Admin")
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var tokenBranch = context.User.FindFirst("branch_id")?.Value;
            var headerBranch = _http.HttpContext?.Request.Headers["X-Branch-Id"].FirstOrDefault();

            if (!Guid.TryParse(tokenBranch, out var tb) || !Guid.TryParse(headerBranch, out var hb))
                return Task.CompletedTask; // no pasa

            if (tb == hb) context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string AdminOrBranchAdmin = "AdminOrBranchAdmin";
        public const string RequireBranchScope = "RequireBranchScope";

        // Builder helpers
        public static AuthorizationOptions AddBookingAutoPolicies(this AuthorizationOptions opt)
        {
            opt.AddPolicy(AdminOnly, p => p.RequireAssertion(c => (c.User.FindFirst("role")?.Value ?? "Staff") == "Admin"));

            opt.AddPolicy(AdminOrBranchAdmin, p => p.AddRequirements(new AdminOrBranchAdminRequirement()));

            opt.AddPolicy(RequireBranchScope, p => p.AddRequirements(new BranchScopeRequirement()));

            // Permisos “granulares” se añaden ad-hoc en Program.cs (ver abajo)
            return opt;
        }
    }
}
