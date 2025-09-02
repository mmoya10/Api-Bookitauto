// ResourceUpdateRequest.cs
namespace WebApi.DTOs.Resources
{
    public sealed class ResourceUpdateRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public int? TotalQuantity { get; init; }
    }
}
