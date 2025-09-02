// ExtraUpdateRequest.cs
namespace WebApi.DTOs.Services
{
    public sealed class ExtraUpdateRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal? Price { get; init; }
        public int? DurationMin { get; init; }
        public bool? Active { get; init; }
        public string? ImageUrl { get; init; }
    }
}
