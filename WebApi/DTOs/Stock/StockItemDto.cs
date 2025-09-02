// StockItemDto.cs
namespace WebApi.DTOs.Stock
{
    public sealed class StockItemDto
    {
        public Guid ProductId { get; init; }
        public string Sku { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? CategoryName { get; init; }
        public int MinStock { get; init; }
        public int CurrentStock { get; init; }
        public bool BelowMin => CurrentStock < MinStock;
        public bool Active { get; init; }
        public string? ImageUrl { get; init; }
    }
}
