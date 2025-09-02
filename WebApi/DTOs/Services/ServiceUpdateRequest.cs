// ServiceUpdateRequest.cs  (si ya lo tienes, déjalo)
namespace WebApi.DTOs.Services
{
    public sealed class ServiceUpdateRequest
    {
        public Guid? CategoryId { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal? BasePrice { get; init; }
        public int? DurationMin { get; init; }
        public int? BufferBefore { get; init; }
        public int? BufferAfter { get; init; }
        public bool? RequiresResource { get; init; }
        public bool? Active { get; init; }
        public string? ImageUrl { get; init; }
    }
}
