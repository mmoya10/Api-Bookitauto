using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IAuthService
    {
        Task<(Staff staff, Role? role, List<string> permissions)?> ValidateStaffAsync(string login, string password, CancellationToken ct);

        Task<(bool ok, string? tokenForTesting)> CreatePasswordResetAsync(string login, TimeSpan ttl, CancellationToken ct);

        Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct);
    }

    public sealed class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes); // AABBCC...
        }

        private static string NewUrlSafeToken(int byteLength = 32)
        {
            var bytes = new byte[byteLength];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('='); // base64url
        }

        public AuthService(AppDbContext db) => _db = db;

        public async Task<(Staff staff, Role? role, List<string> permissions)?> ValidateStaffAsync(string login, string password, CancellationToken ct)
        {
            var staff = await _db.Staff
                .Include(s => s.Branch)
                .Include(s => s.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .Include(s => s.StaffPermissions).ThenInclude(sp => sp.Permission)
                .FirstOrDefaultAsync(s =>
                    (s.Email != null && s.Email == login) || (s.Username != null && s.Username == login),
                    ct);

            if (staff is null || string.IsNullOrWhiteSpace(staff.PasswordHash))
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, staff.PasswordHash))
                return null;

            var permSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (staff.Role?.RolePermissions is not null)
                foreach (var rp in staff.Role.RolePermissions)
                    permSet.Add(rp.Permission.Name);

            if (staff.StaffPermissions is not null)
                foreach (var sp in staff.StaffPermissions.Where(p => p.Active))
                    permSet.Add(sp.Permission.Name);

            return (staff, staff.Role, permSet.ToList());
        }

        public async Task<(bool ok, string? tokenForTesting)> CreatePasswordResetAsync(string login, TimeSpan ttl, CancellationToken ct)
        {
            var staff = await _db.Staff.FirstOrDefaultAsync(s =>
                (s.Email != null && s.Email == login) || (s.Username != null && s.Username == login), ct);

            if (staff is null || string.IsNullOrWhiteSpace(staff.Email))
                return (false, null); // no revelamos si existe o no

            // invalidar tokens anteriores del staff (opcional)
            var now = DateTimeOffset.UtcNow;
            var oldTokens = _db.PasswordResets.Where(p => p.StaffId == staff.Id && p.UsedAt == null && p.ExpiresAt > now);
            _db.PasswordResets.RemoveRange(oldTokens);

            var token = NewUrlSafeToken();
            var hash = HashToken(token);

            var reset = new PasswordReset
            {
                Id = Guid.NewGuid(),
                StaffId = staff.Id,
                TokenHash = hash,
                ExpiresAt = now.Add(ttl),
                CreatedAt = now
            };

            _db.PasswordResets.Add(reset);
            await _db.SaveChangesAsync(ct);

            // TODO: enviar email/SMS con el token (enlace tipo: https://tuapp/reset?token=XYZ)
            // Por ahora, devolvemos el token para pruebas (SOLO en dev).
            return (true, token);
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct)
        {
            var hash = HashToken(token);
            var now = DateTimeOffset.UtcNow;

            var reset = await _db.PasswordResets
                .Include(r => r.Staff)
                .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

            if (reset is null) return false;
            if (reset.UsedAt != null) return false;
            if (reset.ExpiresAt <= now) return false;

            // actualizar password
            reset.Staff.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            reset.UsedAt = now;

            // opcional: limpiar otros tokens activos del mismo staff
            var others = _db.PasswordResets.Where(p => p.StaffId == reset.StaffId && p.Id != reset.Id && p.UsedAt == null);
            _db.PasswordResets.RemoveRange(others);

            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
