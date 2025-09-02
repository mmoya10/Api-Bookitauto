// StaffUpdateRequest.cs
namespace WebApi.DTOs.Staff
{
    public sealed class StaffUpdateRequest
    {
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Password { get; init; } // opcional
        public Guid? RoleId { get; init; }
        public bool? AvailableForBooking { get; init; }
        public bool? IsManager { get; init; }
        public string? PhotoUrl { get; init; }
        public string? Status { get; init; } // active|inactive
    }
}
