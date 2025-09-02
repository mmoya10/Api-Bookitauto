// BookingSiteStepDto.cs
namespace WebApi.DTOs.BookingSites
{
    public sealed class BookingSiteStepDto
    {
        public Guid Id { get; init; }
        public string Step { get; init; } = null!; // service|staff|date|extras|form
        public int Position { get; init; }
    }
}

