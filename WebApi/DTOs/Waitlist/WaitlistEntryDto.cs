// WaitlistEntryDto.cs
namespace WebApi.DTOs.Waitlist
{
    public sealed class WaitlistEntryDto
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; init; }
        public Guid ServiceId { get; init; }
        public Guid? ServiceOptionId { get; init; }
        public Guid? StaffId { get; init; }
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
        public string? Comments { get; init; }
        public bool AutoBook { get; init; }
        public string Status { get; init; } = "active";   // active|cancelled|matched
        public DateTimeOffset CreatedAt { get; init; }
    }
}
