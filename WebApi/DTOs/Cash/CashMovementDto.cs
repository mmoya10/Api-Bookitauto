// CashMovementDto.cs
namespace WebApi.DTOs.Cash
{
    public sealed class CashMovementDto
    {
        public Guid Id { get; init; }
        public Guid SessionId { get; init; }
        public DateTimeOffset Date { get; init; }
        public string Type { get; init; } = null!; // income|expense|adjustment
        public string? Reason { get; init; }
        public decimal Total { get; init; }
        public Guid? SaleId { get; init; }
        public Guid? ExpenseId { get; init; }
        public string? Note { get; init; }
    }
}
