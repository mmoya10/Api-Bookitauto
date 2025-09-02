// WebApi/DTOs/Products/ProductCreateRequest.cs
namespace WebApi.DTOs.Products
{
    public sealed class ProductCreateRequest
    {
        public Guid? CategoryId { get; init; }
        public string Sku { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public decimal? OfferPrice { get; init; }
        public DateTimeOffset? OfferStart { get; init; }
        public DateTimeOffset? OfferEnd { get; init; }
        public bool Active { get; init; } = true;
        public string? ImageUrl { get; init; }
    }
}
