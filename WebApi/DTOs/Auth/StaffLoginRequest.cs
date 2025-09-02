namespace WebApi.DTOs.Auth
{
    public sealed class StaffLoginRequest
    {
        public string Login { get; init; } = null!; // email o username
        public string Password { get; init; } = null!;
    }
}
