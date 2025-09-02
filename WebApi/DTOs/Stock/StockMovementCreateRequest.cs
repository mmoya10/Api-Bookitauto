// StockMovementCreateRequest.cs
namespace WebApi.DTOs.Stock
{
    public sealed class StockMovementCreateRequest
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }                    // puede ser negativo
        public string Type { get; init; } = "purchase";       // purchase|adjustment|sale
        public decimal? TotalPrice { get; init; }
        public string? Notes { get; init; }
        public string? ReferenceType { get; init; }           // sale|expense (opcional)
        public Guid? ReferenceId { get; init; }
    }
}
