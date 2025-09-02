// StockMovementDto.cs
namespace WebApi.DTOs.Stock
{
    public sealed class StockMovementDto
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public string Sku { get; init; } = null!;
        public string ProductName { get; init; } = null!;
        public int Quantity { get; init; }               // +/- 
        public string Type { get; init; } = null!;       // purchase|adjustment|sale
        public decimal? TotalPrice { get; init; }
        public string? Notes { get; init; }
        public string? ReferenceType { get; init; }      // sale|expense
        public Guid? ReferenceId { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
