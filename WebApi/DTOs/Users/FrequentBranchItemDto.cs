// FrequentBranchItemDto.cs
namespace WebApi.DTOs.Users
{
    public sealed class FrequentBranchItemDto
    {
        public Guid BranchId { get; init; }
        public Guid BusinessId { get; init; }
        public string BranchName { get; init; } = null!;
        public string? City { get; init; }
        public string? Province { get; init; }
        public DateTimeOffset? LastBookingAt { get; init; }
    }
}
