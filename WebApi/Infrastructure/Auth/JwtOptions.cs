namespace WebApi.Infrastructure.Auth
{
    public sealed class JwtOptions
    {
        public string Issuer { get; init; } = "";
        public string Audience { get; init; } = "";
        public string Key { get; init; } = "";
        public int AccessTokenMinutes { get; init; } = 120;
    }
}
