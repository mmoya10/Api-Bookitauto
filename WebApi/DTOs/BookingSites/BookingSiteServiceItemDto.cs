// BookingSiteServiceItemDto.cs
namespace WebApi.DTOs.BookingSites
{
    public sealed class BookingSiteServiceItemDto
    {
        public Guid ServiceId { get; init; }
        public string ServiceName { get; init; } = null!;
        public bool Active { get; init; }
        public int Position { get; init; }
    }
}
