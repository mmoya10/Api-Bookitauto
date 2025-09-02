namespace WebApi.DTOs.Common
{
    public sealed class ApiError
    {
        public string Code { get; init; } = "error";   // ej: not_found, bad_request, unauthorized
        public string Message { get; init; } = null!;  // mensaje legible
        public object? Details { get; init; }          // solo en Development
    }
}
