// ExtraCreateRequest.cs
namespace WebApi.DTOs.Services
{
    public sealed class ExtraCreateRequest
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public decimal? Price { get; init; }
        public int? DurationMin { get; init; }
        public bool Active { get; init; } = true;
        public string? ImageUrl { get; init; }
    }
}
