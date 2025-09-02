using Microsoft.AspNetCore.Http;

namespace WebApi.Infrastructure.Filters
{
    public static class BranchContext
    {
        public static Guid GetBranchId(HttpContext ctx)
        {
            if (ctx.Items.TryGetValue(RequireBranchHeaderFilter.ItemKey, out var v) && v is Guid g) return g;
            // fallback: por si quieres leer directo del header
            if (ctx.Request.Headers.TryGetValue(RequireBranchHeaderFilter.HeaderName, out var raw) && Guid.TryParse(raw.FirstOrDefault(), out g)) return g;
            throw new InvalidOperationException("BranchId no disponible en el contexto.");
        }
    }
}
