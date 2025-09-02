// ServiceOptionUpdateRequest.cs
namespace WebApi.DTOs.Services
{
    public sealed class ServiceOptionUpdateRequest
    {
        public string? Name { get; init; }
        public decimal? PriceDelta { get; init; }
        public int? DurationDelta { get; init; }
        public string? ImageUrl { get; init; }
    }
}
