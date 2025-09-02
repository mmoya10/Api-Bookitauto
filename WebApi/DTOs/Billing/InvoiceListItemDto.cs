// InvoiceListItemDto.cs
namespace WebApi.DTOs.Billing
{
    public sealed class InvoiceListItemDto
    {
        public Guid Id { get; init; }
        public DateTimeOffset IssuedAt { get; init; }
        public decimal Total { get; init; }
        public string Currency { get; init; } = "EUR";
        public string Status { get; init; } = "paid";     // paid|open|void|uncollectible
        public string? ExternalId { get; init; }
    }
}
