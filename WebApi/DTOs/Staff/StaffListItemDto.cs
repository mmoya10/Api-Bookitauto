// StaffListItemDto.cs
namespace WebApi.DTOs.Staff
{
    public sealed class StaffListItemDto
    {
        public Guid Id { get; init; }
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string Role { get; init; } = "staff";
        public bool AvailableForBooking { get; init; }
        public bool IsManager { get; init; }
        public string Status { get; init; } = "active";
        public string? PhotoUrl { get; init; }
    }
}
