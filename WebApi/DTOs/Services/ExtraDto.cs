// ExtraDto.cs
namespace WebApi.DTOs.Services
{
    public sealed class ExtraDto
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public decimal? Price { get; init; }
        public int? DurationMin { get; init; }
        public bool Active { get; init; }
        public string? ImageUrl { get; init; }
    }
}
