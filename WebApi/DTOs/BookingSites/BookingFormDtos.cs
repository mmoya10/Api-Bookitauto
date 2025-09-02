// BookingFormDtos.cs
namespace WebApi.DTOs.BookingSites
{
    public sealed class BookingFormDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public int Version { get; init; }
        public bool Active { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    public sealed class BookingFormCreateRequest
    {
        public string Name { get; init; } = null!;
        public bool Active { get; init; } = true; // si true, desactiva otros
    }

    public sealed class BookingFormFieldCreateRequest
    {
        public string Type { get; init; } = "text"; // text|textarea|number|select|checkbox|date|email|phone
        public string FieldName { get; init; } = null!;
        public string Label { get; init; } = null!;
        public bool Required { get; init; }
        public string? HelpText { get; init; }
        public string? ValidationRegex { get; init; }
        public object? Options { get; init; } // se serializa a jsonb
        public int Position { get; init; }
    }

    public sealed class BookingFormFieldUpdateRequest
    {
        public string? Label { get; init; }
        public bool? Required { get; init; }
        public string? HelpText { get; init; }
        public string? ValidationRegex { get; init; }
        public object? Options { get; init; }
        public int? Position { get; init; }
    }
}
