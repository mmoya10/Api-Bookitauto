using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.Infrastructure.Filters
{
    // Valida que venga el X-Branch-Id y lo deja accesible en HttpContext.Items["BranchId"]
    public sealed class RequireBranchHeaderFilter : IAsyncActionFilter
    {
        public const string HeaderName = "X-Branch-Id";
        public const string ItemKey = "BranchId";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;

            if (!http.Request.Headers.TryGetValue(HeaderName, out var raw) ||
                !Guid.TryParse(raw.FirstOrDefault(), out var branchId))
            {
                context.Result = new BadRequestObjectResult(new { error = $"Missing or invalid header {HeaderName}" });
                return;
            }

            http.Items[ItemKey] = branchId;
            await next();
        }
    }
}
