using System.Security.Claims;

namespace WebApi.Infrastructure.Auth
{
    public interface ICurrentUser
    {
        // ← AÑADIR esta propiedad
        Guid? UserId { get; }          // Id del usuario (cliente final) si el token es de app

        Guid? StaffId { get; }         // Id del staff si el token es del panel
        string Role { get; }           // Admin | AdminBranch | Staff | Platform
        Guid? BranchId { get; }        // null si Platform o token de cliente
        bool HasPermission(string permission);
        bool IsPlatform { get; }
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
            Guid.TryParse(_user.FindFirst("branch_id")?.Value, out var g) ? g : null;

        public bool IsPlatform =>
            string.Equals(Role, "Platform", StringComparison.OrdinalIgnoreCase);

        public bool HasPermission(string permission) =>
            _user.FindAll("perm").Any(c => string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}
