using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Dashboard;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryResponse> GetSummaryAsync(Guid branchId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    }

    public sealed class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;
        public DashboardService(AppDbContext db) => _db = db;

        public async Task<DashboardSummaryResponse> GetSummaryAsync(Guid branchId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
        {
            // Rango por defecto: últimos 30 días
            var now = DateTimeOffset.UtcNow;
            var toDt = to ?? now;
            var fromDt = from ?? toDt.AddDays(-30);

            // BOOKINGS
            var bookingsQ = _db.Bookings.AsNoTracking()
                .Where(b => b.BranchId == branchId && b.StartTime >= fromDt && b.StartTime <= toDt);

            var bookingsAgg = await bookingsQ
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    total = g.Count(),
                    completed = g.Count(x => x.Status == "completed"),
                    cancelled = g.Count(x => x.Status == "cancelled"),
                    noShow = g.Count(x => x.Status == "no_show" || x.Status == "no_show"), // por si normalizas
                    upcoming = g.Count(x => x.Status == "confirmed" || x.Status == "pending")
                })
                .FirstOrDefaultAsync(ct) ?? new { total = 0, completed = 0, cancelled = 0, noShow = 0, upcoming = 0 };

            // REVENUE (Sales)
            var salesQ = _db.Sales.AsNoTracking()
                .Where(s => s.BranchId == branchId && s.CreatedAt >= fromDt && s.CreatedAt <= toDt);

            var revenueAgg = await salesQ
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    total = g.Sum(x => (decimal?)x.Total) ?? 0m,
                    services = g.Where(x => x.Type == "service").Sum(x => (decimal?)x.Total) ?? 0m,
                    products = g.Where(x => x.Type == "product").Sum(x => (decimal?)x.Total) ?? 0m
                })
                .FirstOrDefaultAsync(ct) ?? new { total = 0m, services = 0m, products = 0m };

            // TOP SERVICES (por nº de bookings completados y revenue asociado si hay venta)
            var topServices = await _db.Bookings.AsNoTracking()
                .Where(b => b.BranchId == branchId && b.StartTime >= fromDt && b.StartTime <= toDt && b.Status == "completed")
                .GroupBy(b => new { b.ServiceId, b.Service.Name })
                .Select(g => new DashboardSummaryResponse.TopServiceItem
                {
                    ServiceId = g.Key.ServiceId,
                    Name = g.Key.Name,
                    Count = g.Count(),
                    Revenue = g.Sum(x => (decimal?)x.TotalPrice) ?? 0m
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync(ct);

            // NUEVOS USUARIOS (si quieres limitar a los que han reservado en branch, filtra por UserBusiness.BranchId)
            var newUsers = await _db.UserBusinesses.AsNoTracking()
                .Where(ub => ub.BranchId == branchId && ub.StartDate >= fromDt && ub.StartDate <= toDt)
                .CountAsync(ct);

            // WAITLIST
            var waitlistActive = await _db.WaitlistEntries.AsNoTracking()
                .CountAsync(w => w.BranchId == branchId && w.Status == "active", ct);

            return new DashboardSummaryResponse
            {
                From = fromDt,
                To = toDt,
                BookingsTotal = bookingsAgg.total,
                BookingsCompleted = bookingsAgg.completed,
                BookingsCancelled = bookingsAgg.cancelled,
                BookingsNoShow = bookingsAgg.noShow,
                BookingsUpcoming = bookingsAgg.upcoming,
                RevenueTotal = revenueAgg.total,
                RevenueServices = revenueAgg.services,
                RevenueProducts = revenueAgg.products,
                NewUsers = newUsers,
                WaitlistActive = waitlistActive,
                TopServices = topServices
            };
        }
    }
}
