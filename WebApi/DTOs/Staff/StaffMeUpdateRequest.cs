// StaffMeUpdateRequest.cs
namespace WebApi.DTOs.Staff
{
    public sealed class StaffMeUpdateRequest
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Phone { get; init; }
        public string? PhotoUrl { get; init; }

        // Cambio de contraseña (opcional)
        public string? CurrentPassword { get; init; }
        public string? NewPassword { get; init; }
    }
}
