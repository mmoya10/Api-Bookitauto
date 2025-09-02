using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Auth;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Services;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IJwtTokenService _jwt;
        private readonly AppDbContext _db;
        private readonly IHostEnvironment _env;

        public AuthController(IAuthService auth, IJwtTokenService jwt, AppDbContext db, IHostEnvironment env)
        {
            _auth = auth; _jwt = jwt; _db = db; _env = env;
        }

        /// <summary>
        /// Staff login (no hay registro desde fuera).
        /// Devuelve staff/rol/permisos y, si role=Admin, listado de branches (id,name,slug).
        /// </summary>
        [HttpPost("staff/login")]
        public async Task<ActionResult<AuthResponse>> StaffLogin([FromBody] StaffLoginRequest req, CancellationToken ct)
        {
            var res = await _auth.ValidateStaffAsync(req.Login, req.Password, ct);
            if (res is null) return Unauthorized(new { error = "Credenciales inválidas" });

            var (staff, role, perms) = res.Value;
            var token = _jwt.CreateStaffToken(staff, role, perms);

            object[]? branches = null;

            // Si es Admin, cargamos branches del mismo Business que su branch actual
            var roleName = (role?.Name ?? "staff").Trim().ToLowerInvariant();
            if (roleName == "admin")
            {
                // staff.Branch está incluido en ValidateStaffAsync
                var businessId = staff.Branch.BusinessId;

                branches = await _db.Branches
                    .Where(b => b.BusinessId == businessId && (b.Status == "active" || b.Status == "inactive"))
                    .OrderBy(b => b.Name)
                    .Select(b => new { id = b.Id, name = b.Name, slug = b.Slug, status = b.Status })
                    .ToArrayAsync(ct);
            }

            return Ok(new AuthResponse
            {
                Token = token,
                Role = role?.Name ?? "staff",
                Permissions = perms,
                Staff = new
                {
                    id = staff.Id,
                    branchId = staff.BranchId,
                    username = staff.Username,
                    email = staff.Email,
                    firstName = staff.FirstName,
                    lastName = staff.LastName,
                    isManager = staff.IsManager,
                    photoUrl = staff.PhotoUrl,
                    status = staff.Status
                },
                Branches = branches
            });
        }

        /// <summary>
        /// Forgot password para staff. No revela si existe el usuario.
        /// En desarrollo devuelve el token para pruebas.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequest req, CancellationToken ct)
        {
            // TTL de 30 min
            var (ok, token) = await _auth.CreatePasswordResetAsync(req.Login, TimeSpan.FromMinutes(30), ct);

            if (_env.IsDevelopment() && ok && token is not null)
                return Ok(new { message = "Si existe un usuario con ese login, se ha enviado un email con instrucciones.", devToken = token });

            return Ok(new { message = "Si existe un usuario con ese login, se ha enviado un email con instrucciones." });
        }

        /// <summary>
        /// Reset password (con token enviado por email).
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest req, CancellationToken ct)
        {
            var ok = await _auth.ResetPasswordAsync(req.Token, req.NewPassword, ct);
            if (!ok) return BadRequest(new { error = "Token inválido o expirado" });

            return Ok(new { message = "Contraseña actualizada" });
        }

        // Eliminamos endpoints de registro/login de cliente final en este controller (serán otros)
    }
}
