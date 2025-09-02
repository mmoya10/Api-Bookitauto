namespace WebApi.DTOs.Auth
{
    public sealed class ForgotPasswordRequest
    {
        public string Login { get; init; } = null!; // email o username
    }
}
