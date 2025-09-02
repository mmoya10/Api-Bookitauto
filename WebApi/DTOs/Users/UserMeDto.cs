// UserMeDto.cs
namespace WebApi.DTOs.Users
{
    public sealed class UserMeDto
    {
        public Guid Id { get; init; }
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? PhotoUrl { get; init; }
        public bool Active { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
