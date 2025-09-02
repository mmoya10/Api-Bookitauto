// WebApi/DTOs/Products/ProductUpdateRequest.cs
namespace WebApi.DTOs.Products
{
    public sealed class ProductUpdateRequest
    {
        public Guid? CategoryId { get; init; }
        public string? Sku { get; init; }              // opcional; si viene, se valida unicidad
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal? Price { get; init; }
        public decimal? OfferPrice { get; init; }
        public DateTimeOffset? OfferStart { get; init; }
        public DateTimeOffset? OfferEnd { get; init; }
        public bool? Active { get; init; }
        public string? ImageUrl { get; init; }
    }
}
