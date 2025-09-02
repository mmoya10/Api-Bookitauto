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
        string CreateStaffToken(Staff staff, Role? role, IEnumerable<string> permissionNames);
    }

    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;

        public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

        public string CreateStaffToken(Staff staff, Role? role, IEnumerable<string> permissionNames)
        {
            var now = DateTimeOffset.UtcNow;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, staff.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, staff.Id.ToString()),
                new(ClaimTypes.Name, staff.Username ?? staff.Email ?? staff.Id.ToString()),
                new("role", NormalizeRole(role?.Name ?? "staff")),                 // Admin | AdminBranch | Staff
                new("branch_id", staff.BranchId.ToString()),                       // alcance por sucursal
                new("is_manager", staff.IsManager ? "1" : "0"),
                new("status", staff.Status)
            };

            // permissions -> varios "perm:XXX"
            claims.AddRange(permissionNames.Select(p => new Claim("perm", p)));

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
                _ => "Staff"
            };
        }
    }
}
