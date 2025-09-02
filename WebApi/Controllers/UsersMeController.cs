using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Users;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/users/me")]
    [Authorize] // token de cliente final
    public sealed class UsersMeController : ControllerBase
    {
        private readonly IUsersMeService _svc;
        public UsersMeController(IUsersMeService svc) => _svc = svc;

        // 👤 Devuelve tu perfil (datos básicos)
        [HttpGet]
        public async Task<IActionResult> Me(CancellationToken ct)
            => Ok(new { me = await _svc.GetAsync(ct) });

        // ✏️ Actualiza tu perfil (nombre, email, teléfono, foto...)
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UserMeUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(req, ct);
            return Ok(new { ok = true });
        }

        // ⭐ Sucursales más frecuentes para ti (por última reserva)
        [HttpGet("branches/frequent")]
        public async Task<IActionResult> Frequent(CancellationToken ct)
            => Ok(new { items = await _svc.GetFrequentBranchesAsync(take: 8, ct) });

        // 📅 Tus reservas (status=upcoming|past|<status exacto> opcional)
        [HttpGet("bookings")]
        public async Task<IActionResult> MyBookings([FromQuery] string? status, CancellationToken ct)
            => Ok(new { items = await _svc.GetMyBookingsAsync(status, ct) });
    }
}
