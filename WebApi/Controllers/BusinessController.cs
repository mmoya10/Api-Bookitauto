using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Business;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/businesses")]
    [Authorize(Roles = "Admin")] // Solo Admin global
    public sealed class BusinessController : ControllerBase
    {
        private readonly IBusinessService _svc;
        public BusinessController(IBusinessService svc) => _svc = svc;

        // 🔎 Obtiene el detalle de un negocio (datos generales)
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => Ok(await _svc.GetAsync(id, ct));

        // ✏️ Actualiza un negocio (contacto, estado, slug, etc.)
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] BusinessUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        // 🏬 Lista las sucursales asociadas al negocio
        [HttpGet("{id:guid}/branches")]
        public async Task<IActionResult> Branches(Guid id, CancellationToken ct)
            => Ok(new { items = await _svc.GetBranchesAsync(id, ct) });
    }
}
