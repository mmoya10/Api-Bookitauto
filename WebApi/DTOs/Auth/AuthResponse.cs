namespace WebApi.DTOs.Auth
{
    public sealed class AuthResponse
    {
        public string Token { get; init; } = null!;
        public string Role { get; init; } = null!;
        public IEnumerable<string> Permissions { get; init; } = Array.Empty<string>();
        public object Staff { get; init; } = null!;
        public object[]? Branches { get; init; }      // solo cuando Role == "Admin"
    }
}
