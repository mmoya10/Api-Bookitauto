using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Services;
using WebApi.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>();

// JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var cfg = builder.Configuration.GetSection("Jwt");
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg["Issuer"],
            ValidateAudience = true,
            ValidAudience = cfg["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddBookingAutoPolicies();

    // CASH
    opt.AddPolicy("Perm.Cash.View",    p => p.Requirements.Add(new PermissionRequirement(Permissions.Cash.View)));
    opt.AddPolicy("Perm.Cash.Manage",  p => p.Requirements.Add(new PermissionRequirement(Permissions.Cash.Manage)));

    // PRODUCTS
    opt.AddPolicy("Perm.Products.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Products.View)));
    opt.AddPolicy("Perm.Products.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Products.Manage)));

    // STOCK
    opt.AddPolicy("Perm.Stock.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Stock.View)));
    opt.AddPolicy("Perm.Stock.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Stock.Manage)));

    // RESOURCES (ESPACIOS)
    opt.AddPolicy("Perm.Resources.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Resources.View)));
    opt.AddPolicy("Perm.Resources.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Resources.Manage)));

    // CALENDARS
    opt.AddPolicy("Perm.Calendars.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Calendars.View)));
    opt.AddPolicy("Perm.Calendars.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Calendars.Manage)));

    // BOOKINGS
    opt.AddPolicy("Perm.Bookings.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Bookings.View)));
    opt.AddPolicy("Perm.Bookings.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Bookings.Manage)));

    // USERS (opcional)
    opt.AddPolicy("Perm.Users.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Users.View)));
    opt.AddPolicy("Perm.Users.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Users.Manage)));
});


// Handlers + helpers
builder.Services.AddSingleton<IAuthorizationHandler, AdminOrBranchAdminHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, BranchScopeHandler>();

// Servicios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IStaffProfileService, StaffProfileService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ICashService, CashService>();
builder.Services.AddScoped<IBookingOpsService, BookingOpsService>();
builder.Services.AddScoped<IBookingSiteService, BookingSiteService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IAbsenceService, AbsenceService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IUsersMeService, UsersMeService>();
builder.Services.AddScoped<IPublicService, PublicService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Payments.IPaymentWebhookService, WebApi.Infrastructure.Services.Payments.PaymentWebhookService>();
// Services Platform
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IPlatformAccountService, WebApi.Infrastructure.Services.Platform.PlatformAccountService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IPlatformTicketService, WebApi.Infrastructure.Services.Platform.PlatformTicketService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IPlatformSubscriptionService, WebApi.Infrastructure.Services.Platform.PlatformSubscriptionService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IImpersonationService, WebApi.Infrastructure.Services.Platform.ImpersonationService>();


// Servicios de notificación
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.INotificationConfigService, WebApi.Infrastructure.Services.Notification.NotificationConfigService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.IEmailSender, WebApi.Infrastructure.Services.Notification.SmtpEmailSender>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.IPushSender, WebApi.Infrastructure.Services.Notification.NoopPushSender>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.INotificationTemplateService, WebApi.Infrastructure.Services.Notification.NotificationTemplateService>();

// Hosted services (jobs)
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.BookingReminderScheduler>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.NotificationDispatcher>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.CashAutoCloseJob>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.HoldSlotSweeper>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.SubscriptionBillingJob>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelValidationFilter>();
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<RequireBranchHeaderFilter>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
