// ServiceOptionCreateRequest.cs
namespace WebApi.DTOs.Services
{
    public sealed class ServiceOptionCreateRequest
    {
        public string Name { get; init; } = null!;
        public decimal PriceDelta { get; init; }
        public int DurationDelta { get; init; }
        public string? ImageUrl { get; init; }
    }
}
