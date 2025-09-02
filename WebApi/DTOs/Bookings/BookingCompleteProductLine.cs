namespace WebApi.DTOs.Bookings
{
    public sealed class BookingCompleteProductLine
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }              // >0
        public decimal UnitPrice { get; init; }         // >=0
        public decimal Total { get; init; }             // Quantity * UnitPrice
    }
}
