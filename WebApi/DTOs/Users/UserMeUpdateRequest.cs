// UserMeUpdateRequest.cs
namespace WebApi.DTOs.Users
{
    public sealed class UserMeUpdateRequest
    {
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? PhotoUrl { get; init; }
    }
}
