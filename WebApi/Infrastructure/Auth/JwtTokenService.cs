using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Auth
{
    public interface IJwtTokenService
    {
        // 👈 añadimos isPlatform para emitir tokens sin branch_id
        string CreateStaffToken(Staff staff, Role? role, IEnumerable<string> permissionNames, bool isPlatform = false);
    }

    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

        public string CreateStaffToken(Staff staff, Role? role, IEnumerable<string> permissionNames, bool isPlatform = false)
        {
            var now = DateTimeOffset.UtcNow;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, staff.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, staff.Id.ToString()),
                new(ClaimTypes.Name, staff.Username ?? staff.Email ?? staff.Id.ToString()),
                new("role", isPlatform ? "Platform" : NormalizeRole(role?.Name ?? "staff")),
                new("is_manager", staff.IsManager ? "1" : "0"),
                new("status", staff.Status)
            };

            // Solo los no-Platform llevan alcance de branch en el token
            if (!isPlatform)
                claims.Add(new Claim("branch_id", staff.BranchId.ToString()));

            foreach (var p in permissionNames)
                claims.Add(new Claim("perm", p));

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)),
                SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: now.AddMinutes(_opt.AccessTokenMinutes).UtcDateTime,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private static string NormalizeRole(string name)
        {
            var n = (name ?? "").Trim().ToLowerInvariant();
            return n switch
            {
                "admin" => "Admin",
                "adminbranch" or "branchadmin" or "manager" => "AdminBranch",
                "platform" or "staffempresa" or "owner" => "Platform",
                _ => "Staff"
            };
        }
    }
}
