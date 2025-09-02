// Infrastructure/Services/Platform/ImpersonationService.cs
using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services.Platform
{
    public interface IImpersonationService
    {
        Task<(string token, Staff staff)> ImpersonateAsync(Guid staffId, string? reason, string? ip, string? ua, CancellationToken ct);
    }

    public sealed class ImpersonationService : IImpersonationService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUser _me;
        private readonly IJwtTokenService _jwt;

        public ImpersonationService(AppDbContext db, ICurrentUser me, IJwtTokenService jwt)
        {
            _db = db; _me = me; _jwt = jwt;
        }

        public async Task<(string token, Staff staff)> ImpersonateAsync(Guid staffId, string? reason, string? ip, string? ua, CancellationToken ct)
        {
            if (!_me.IsPlatform) throw new UnauthorizedAccessException("Solo Platform puede impersonar.");

            var staff = await _db.Staff
                .Include(s => s.Branch)
                .Include(s => s.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .Include(s => s.StaffPermissions).ThenInclude(sp => sp.Permission)
                .FirstOrDefaultAsync(s => s.Id == staffId, ct)
                ?? throw new KeyNotFoundException("Staff no encontrado.");

            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (staff.Role?.RolePermissions != null)
                foreach (var rp in staff.Role.RolePermissions) perms.Add(rp.Permission.Name);
            if (staff.StaffPermissions != null)
                foreach (var sp in staff.StaffPermissions.Where(p => p.Active)) perms.Add(sp.Permission.Name);

            // emitir token como staff (no platform)
            var token = _jwt.CreateStaffToken(staff, staff.Role, perms, isPlatform: false);

            // auditoría
            var bizId = staff.Branch.BusinessId;
            _db.ImpersonationLogs.Add(new ImpersonationLog
            {
                Id = Guid.NewGuid(),
                PlatformStaffId = _me.StaffId ?? Guid.Empty,
                TargetStaffId = staff.Id,
                BusinessId = bizId,
                BranchId = staff.BranchId,
                Reason = reason ?? "support",
                IpAddress = ip,
                UserAgent = ua
            });
            await _db.SaveChangesAsync(ct);

            return (token, staff);
        }
    }
}
