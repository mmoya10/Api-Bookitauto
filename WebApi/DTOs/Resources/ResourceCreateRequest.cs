// ResourceCreateRequest.cs
namespace WebApi.DTOs.Resources
{
    public sealed class ResourceCreateRequest
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public int TotalQuantity { get; init; } = 1;
    }
}
