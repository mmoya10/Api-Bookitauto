// CashSessionCloseRequest.cs
namespace WebApi.DTOs.Cash
{
    public sealed class CashSessionCloseRequest
    {
        public decimal? ExpectedClose { get; init; }
        public string? ClosingNote { get; init; }
    }
}
