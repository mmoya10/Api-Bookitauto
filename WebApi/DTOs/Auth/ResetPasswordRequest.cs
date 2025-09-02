namespace WebApi.DTOs.Auth
{
    public sealed class ResetPasswordRequest
    {
        public string Token { get; init; } = null!;      // token plano recibido por email
        public string NewPassword { get; init; } = null!;
    }
}
