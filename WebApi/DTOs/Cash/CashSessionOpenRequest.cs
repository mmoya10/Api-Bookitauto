// CashSessionOpenRequest.cs
namespace WebApi.DTOs.Cash
{
    public sealed class CashSessionOpenRequest
    {
        public decimal? ExpectedOpen { get; init; }
        public string? OpeningNote { get; init; }
    }
}
