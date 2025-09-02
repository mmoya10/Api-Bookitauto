// WebApi/DTOs/Products/ProductListItemDto.cs
namespace WebApi.DTOs.Products
{
    public sealed class ProductListItemDto
    {
        public Guid Id { get; init; }
        public string Sku { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? CategoryName { get; init; }
        public decimal Price { get; init; }
        public decimal? OfferPrice { get; init; }
        public bool Active { get; init; }
        public string? ImageUrl { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
