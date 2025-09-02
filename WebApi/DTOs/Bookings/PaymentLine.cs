namespace WebApi.DTOs.Bookings
{
    public sealed class PaymentLine
    {
        // "cash" | "card" | "online" | "transfer"
        // (si quisieras "mixed" divide en varias lines)
        public string Method { get; init; } = "cash";
        public decimal Total { get; init; }             // > 0
    }
}
