using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.DTOs.Common;

namespace WebApi.Infrastructure.Filters
{
    public sealed class ModelValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var error = new ApiError
                {
                    Code = "validation_error",
                    Message = "Hay errores de validación en la solicitud.",
                    Details = errors
                };

                context.Result = new BadRequestObjectResult(error);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
