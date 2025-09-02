using Microsoft.AspNetCore.Mvc;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api")]
    public sealed class PublicController : ControllerBase
    {
        private readonly IPublicService _svc;
        public PublicController(IPublicService svc) => _svc = svc;

        // 🔍 Buscar negocios/sucursales públicas por texto/categoría/ciudad (lat/lng reservado para futuro)
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] string? category, [FromQuery] string? city, [FromQuery] double? lat, [FromQuery] double? lng, CancellationToken ct)
            => Ok(new { items = await _svc.SearchAsync(query, category, city, lat, lng, ct) });

        // 🏢 Datos públicos de un negocio (y sus sucursales activas)
        [HttpGet("businesses/{id:guid}")]
        public async Task<IActionResult> Biz(Guid id, CancellationToken ct)
            => Ok(await _svc.GetBusinessAsync(id, ct));

        // 🏬 Datos públicos de una sucursal (contacto, horarios, servicios visibles)
        [HttpGet("branches/{id:guid}")]
        public async Task<IActionResult> Branch(Guid id, CancellationToken ct)
            => Ok(await _svc.GetBranchAsync(id, ct));
    }
}
