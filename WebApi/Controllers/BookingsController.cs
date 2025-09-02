using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Bookings;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches/{branchId:guid}/bookings")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class BookingsController : ControllerBase
    {
        private readonly IBookingOpsService _ops;
        public BookingsController(IBookingOpsService ops) => _ops = ops;

        // Calendario / listar citas
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Bookings.View")]
        public IActionResult List(Guid branchId, [FromQuery] string? status, [FromQuery] Guid? categoryId, [FromQuery] Guid? serviceId, [FromQuery] Guid? staffId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
            => Ok(new { items = Array.Empty<object>() });

        // Crear manual (staff)
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Bookings.Manage")]
        public IActionResult Create(Guid branchId) => Ok(new { id = Guid.NewGuid() });

        // Editar
        [HttpPut("/api/bookings/{id:guid}")]
        [Authorize(Policy = "Perm.Bookings.Manage")]
        public IActionResult Update(Guid id) => Ok(new { id });

        // Cancelar
        [HttpDelete("/api/bookings/{id:guid}")]
        [Authorize(Policy = "Perm.Bookings.Manage")]
        public IActionResult Cancel(Guid id) => Ok(new { id });

        // === Completar cita ===
        [HttpPut("/api/bookings/{id:guid}/complete")]
        [Authorize(Policy = "Perm.Bookings.Manage")]
        [Authorize(Policy = "Perm.Cash.Manage")]   // registra movimientos de caja si hay efectivo
        [Authorize(Policy = "Perm.Stock.Manage")]  // registra movimientos de stock por productos
        public async Task<IActionResult> Complete(Guid id, [FromBody] BookingCompleteRequest req, CancellationToken ct)
        {
            await _ops. CompleteAsync(id, req, ct);
            return Ok(new { id, status = req.Outcome });
        }
    }
}
