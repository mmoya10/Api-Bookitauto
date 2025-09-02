// StaffMeDto.cs
namespace WebApi.DTOs.Staff
{
    public sealed class StaffMeDto
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; init; }
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? PhotoUrl { get; init; }
        public bool AvailableForBooking { get; init; }
        public bool IsManager { get; init; }
        public string Status { get; init; } = "active";
        public string Role { get; init; } = "staff";
    }
}
