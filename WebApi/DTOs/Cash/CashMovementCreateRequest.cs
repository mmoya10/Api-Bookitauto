// CashMovementCreateRequest.cs
namespace WebApi.DTOs.Cash
{
    public sealed class CashMovementCreateRequest
    {
        public string Type { get; init; } = "income"; // income|expense|adjustment
        public string? Reason { get; init; }
        public decimal Total { get; init; }           // > 0
        public Guid? SaleId { get; init; }
        public Guid? ExpenseId { get; init; }
        public string? Note { get; init; }
    }
}
