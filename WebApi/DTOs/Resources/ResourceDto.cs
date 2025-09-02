// ResourceDto.cs
namespace WebApi.DTOs.Resources
{
    public sealed class ResourceDto
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public int TotalQuantity { get; init; }
    }
}
