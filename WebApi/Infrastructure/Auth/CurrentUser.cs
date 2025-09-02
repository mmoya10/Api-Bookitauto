using System.Security.Claims;

namespace WebApi.Infrastructure.Auth
{
    public interface ICurrentUser
    {
        Guid? UserId { get; }   // <-- NUEVO (usuario cliente)
        Guid? StaffId { get; }  // staff (panel interno)
        string Role { get; }    // Admin | AdminBranch | Staff
        Guid? BranchId { get; } // del token
        bool HasPermission(string permission);
    }

    public sealed class CurrentUser : ICurrentUser
    {
        private readonly ClaimsPrincipal _user;

        public CurrentUser(IHttpContextAccessor accessor)
        {
            _user = accessor.HttpContext?.User ?? new ClaimsPrincipal();
        }

        // Usuario (app cliente): claim "user_id"
        public Guid? UserId =>
            Guid.TryParse(_user.FindFirst("user_id")?.Value, out var gu) ? gu : null;

        // Staff (panel): NameIdentifier
        public Guid? StaffId =>
            Guid.TryParse(_user.FindFirstValue(ClaimTypes.NameIdentifier), out var gs) ? gs : null;

        public string Role => _user.FindFirst("role")?.Value ?? "Staff";

        public Guid? BranchId =>
            Guid.TryParse(_user.FindFirst("branch_id")?.Value, out var gb) ? gb : null;

        public bool HasPermission(string permission) =>
            _user.FindAll("perm").Any(c => string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}
