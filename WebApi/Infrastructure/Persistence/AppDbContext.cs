using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<BusinessCategory> BusinessCategories => Set<BusinessCategory>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<BranchSchedule> BranchSchedules => Set<BranchSchedule>();
        public DbSet<BranchException> BranchExceptions => Set<BranchException>();

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<StaffPermission> StaffPermissions => Set<StaffPermission>();
        public DbSet<StaffSchedule> StaffSchedules => Set<StaffSchedule>();
        public DbSet<StaffException> StaffExceptions => Set<StaffException>();

        public DbSet<User> Users => Set<User>();
        public DbSet<UserBusiness> UserBusinesses => Set<UserBusiness>();

        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<CouponUser> CouponUsers => Set<CouponUser>();
        public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();

        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Stock> Stocks => Set<Stock>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleLine> SaleLines => Set<SaleLine>();
        public DbSet<CashSession> CashSessions => Set<CashSession>();
        public DbSet<CashMovement> CashMovements => Set<CashMovement>();

        public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
        public DbSet<Extra> Extras => Set<Extra>();
        public DbSet<ServiceExtra> ServiceExtras => Set<ServiceExtra>();
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<ServiceResource> ServiceResources => Set<ServiceResource>();

        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingExtra> BookingExtras => Set<BookingExtra>();
        public DbSet<BookingResource> BookingResources => Set<BookingResource>();
        public DbSet<HoldSlot> HoldSlots => Set<HoldSlot>();

        public DbSet<BookingSite> BookingSites => Set<BookingSite>();
        public DbSet<BookingSiteService> BookingSiteServices => Set<BookingSiteService>();
        public DbSet<BookingSiteStep> BookingSiteSteps => Set<BookingSiteStep>();

        public DbSet<BookingForm> BookingForms => Set<BookingForm>();
        public DbSet<BookingFormField> BookingFormFields => Set<BookingFormField>();
        public DbSet<BookingFormResponse> BookingFormResponses => Set<BookingFormResponse>();

        public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
        public DbSet<WaitlistEntryExtra> WaitlistEntryExtras => Set<WaitlistEntryExtra>();
        public DbSet<WaitlistNotification> WaitlistNotifications => Set<WaitlistNotification>();
        public DbSet<WaitlistMatchLog> WaitlistMatchLogs => Set<WaitlistMatchLog>();

        public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
        public DbSet<VacationCounter> VacationCounters => Set<VacationCounter>();
        public DbSet<HoursCounter> HoursCounters => Set<HoursCounter>();
        public DbSet<Absence> Absences => Set<Absence>();
        public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();
        public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
        public DbSet<SmtpConfig> SmtpConfigs => Set<SmtpConfig>();
        public DbSet<PaymentProviderConfig> PaymentProviderConfigs => Set<PaymentProviderConfig>();
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();



        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Money helpers
            void Money<T>(string prop) where T : class
                => b.Entity<T>().Property(prop).HasColumnType("numeric(12,2)");

            // jsonb mappings
            b.Entity<BookingFormField>().Property(x => x.Options).HasColumnType("jsonb");
            b.Entity<BookingFormResponse>().Property(x => x.ValueJson).HasColumnType("jsonb");
            b.Entity<WaitlistNotification>().Property(x => x.Details).HasColumnType("jsonb");
            b.Entity<WaitlistMatchLog>().Property(x => x.Context).HasColumnType("jsonb");
            b.Entity<NotificationLog>().Property(x => x.PayloadJson).HasColumnType("jsonb");

            // Money mappings
            Money<Product>(nameof(Product.Price));
            b.Entity<Product>().Property(x => x.OfferPrice).HasColumnType("numeric(12,2)");
            Money<StockMovement>(nameof(StockMovement.TotalPrice));
            Money<Expense>(nameof(Expense.TotalPrice));
            Money<Sale>(nameof(Sale.Total));
            Money<SaleLine>(nameof(SaleLine.UnitPrice));
            Money<SaleLine>(nameof(SaleLine.Total));
            Money<CashMovement>(nameof(CashMovement.Total));
            Money<Booking>(nameof(Booking.TotalPrice));
            Money<CouponUsage>(nameof(CouponUsage.Discount));
            Money<BookingExtra>(nameof(BookingExtra.Price));
            Money<Coupon>(nameof(Coupon.Value));
            Money<CashSession>(nameof(CashSession.ExpectedOpen));
            Money<CashSession>(nameof(CashSession.ExpectedClose));

            // Unique & indexes
            b.Entity<Business>().HasIndex(x => x.Slug).IsUnique();
            b.Entity<Branch>().HasIndex(x => new { x.BusinessId, x.Slug }).IsUnique();
            b.Entity<BranchSchedule>()
                .ToTable(tb => tb.HasCheckConstraint(
                    "CK_BranchSchedule_Time",
                    "\"EndTime\" > \"StartTime\""));


            b.Entity<Role>().HasIndex(x => x.Name).IsUnique();
            b.Entity<Permission>().HasIndex(x => x.Name).IsUnique();
            b.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
            b.Entity<StaffPermission>().HasIndex(x => new { x.StaffId, x.PermissionId }).IsUnique();

            b.Entity<User>().HasIndex(x => x.Email).IsUnique();
            b.Entity<UserBusiness>().HasIndex(x => new { x.UserId, x.BusinessId }).IsUnique();

            b.Entity<Coupon>().HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            b.Entity<CouponUser>().HasKey(x => new { x.CouponId, x.UserId });

            b.Entity<Product>().HasIndex(x => new { x.BranchId, x.Sku }).IsUnique();
            b.Entity<Stock>().HasIndex(x => x.ProductId).IsUnique();

            // Cash session partial unique (only one open per branch)
            b.Entity<CashSession>()
                .HasIndex(x => x.BranchId)
                .IsUnique()
                .HasFilter("\"ClosedAt\" IS NULL");

            // Service relations
            b.Entity<ServiceExtra>().HasKey(x => new { x.ServiceId, x.ExtraId });
            b.Entity<ServiceResource>().HasKey(x => new { x.ServiceId, x.ResourceId });

            b.Entity<BookingExtra>().HasKey(x => new { x.BookingId, x.ExtraId });
            b.Entity<BookingResource>().HasKey(x => new { x.BookingId, x.ResourceId });

            b.Entity<Booking>().HasIndex(x => new { x.BranchId, x.StartTime });

            b.Entity<Booking>()
                .HasOne(x => x.Sale)
                .WithOne(x => x.Booking)
                .HasForeignKey<Booking>(x => x.SaleId)
                .OnDelete(DeleteBehavior.SetNull);

            // Booking sites
            b.Entity<BookingSite>().HasIndex(x => new { x.BranchId, x.Slug }).IsUnique();
            b.Entity<BookingSite>()
                .HasIndex(x => x.BranchId)
                .HasFilter("\"IsPrimary\" = TRUE")
                .IsUnique();

            b.Entity<BookingSiteService>().HasKey(x => new { x.SiteId, x.ServiceId });
            b.Entity<BookingSiteService>().HasIndex(x => new { x.SiteId, x.Position });

            b.Entity<BookingSiteStep>().HasIndex(x => new { x.SiteId, x.Position }).IsUnique();
            b.Entity<BookingSiteStep>().HasIndex(x => new { x.SiteId, x.Step }).IsUnique();

            // Forms
            b.Entity<BookingForm>().HasIndex(x => new { x.SiteId, x.Version }).IsUnique();
            b.Entity<BookingFormField>().HasIndex(x => new { x.FormId, x.FieldName }).IsUnique();
            b.Entity<BookingFormResponse>().HasIndex(x => new { x.BookingId, x.FieldId }).IsUnique();

            // Waitlist: tstzrange + gist
            b.Entity<WaitlistEntry>().Property(e => e.TimeWindow).HasColumnType("tstzrange");
            b.Entity<WaitlistEntry>().HasIndex(e => e.TimeWindow).HasMethod("gist");
            b.Entity<WaitlistEntry>().HasIndex(e => new { e.BranchId, e.ServiceId, e.Status });
            b.Entity<WaitlistEntryExtra>().HasKey(x => new { x.EntryId, x.ExtraId });

            b.Entity<PasswordReset>()
            .HasIndex(x => new { x.StaffId, x.TokenHash })
            .IsUnique();

            b.Entity<PasswordReset>()
             .HasIndex(x => x.ExpiresAt);
            b.Entity<PaymentEvent>().HasIndex(x => new { x.Provider, x.EventId }).IsUnique();

            b.Entity<PaymentProviderConfig>()
    .HasIndex(x => new { x.BranchId, x.Provider }).IsUnique();

            b.Entity<UserDevice>()
                .HasIndex(x => new { x.UserId, x.DeviceToken }).IsUnique();


            // Default: evitar cascadas peligrosas
            foreach (var entityType in b.Model.GetEntityTypes())
            {
                foreach (var fk in entityType.GetForeignKeys())
                {
                    if (!fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade)
                        fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }
}
