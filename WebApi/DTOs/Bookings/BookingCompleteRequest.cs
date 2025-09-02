namespace WebApi.DTOs.Bookings
{
    public sealed class BookingCompleteRequest
    {
        // "completed" | "no_show"
        public string Outcome { get; init; } = "completed";

        // Si Outcome == completed:
        public decimal ServiceTotal { get; init; }              // total del servicio tras edición (>=0)
        public List<BookingCompleteProductLine> Products { get; init; } = new(); // opcional
        public List<PaymentLine> Payments { get; init; } = new(); // sum debe == ServiceTotal + sum(Product.total)

        // Opcional: observaciones/cambios
        public string? Note { get; init; }
    }
}