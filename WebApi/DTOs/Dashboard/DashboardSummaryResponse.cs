namespace WebApi.DTOs.Dashboard
{
    public sealed class DashboardSummaryResponse
    {
        // Rango consultado (UTC)
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }

        // KPIs
        public int BookingsTotal { get; init; }
        public int BookingsCompleted { get; init; }
        public int BookingsCancelled { get; init; }
        public int BookingsNoShow { get; init; }
        public int BookingsUpcoming { get; init; }

        public decimal RevenueTotal { get; init; }          // ventas (service/product) del rango
        public decimal RevenueServices { get; init; }
        public decimal RevenueProducts { get; init; }

        public int NewUsers { get; init; }                  // usuarios creados en el rango (asociados a ese negocio opcionalmente)
        public int WaitlistActive { get; init; }            // entradas activas de la lista de espera

        public IReadOnlyList<TopServiceItem> TopServices { get; init; } = Array.Empty<TopServiceItem>();

        public sealed class TopServiceItem
        {
            public Guid ServiceId { get; init; }
            public string Name { get; init; } = null!;
            public int Count { get; init; }                 // nº de reservas completadas del servicio
            public decimal Revenue { get; init; }           // suma de ventas ligadas (si procede)
        }
    }
}
