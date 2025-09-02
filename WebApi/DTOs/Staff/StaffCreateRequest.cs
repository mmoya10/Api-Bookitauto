// StaffCreateRequest.cs
namespace WebApi.DTOs.Staff
{
    public sealed class StaffCreateRequest
    {
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Password { get; init; } // plano, se guardará como hash
        public Guid? RoleId { get; init; }
        public bool AvailableForBooking { get; init; } = true;
        public bool IsManager { get; init; } = false;
        public string? PhotoUrl { get; init; }
    }
}
