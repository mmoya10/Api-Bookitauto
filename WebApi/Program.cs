using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Middleware;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Persistence;
using WebApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore; // 👈 arriba

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});
// CORS SPA
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("spa", p => p
        .WithOrigins("http://localhost:5173", "https://tu-dominio-web")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // ✨ Importante: no remapear claims automáticamente
        o.MapInboundClaims = false;

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
            ClockSkew = TimeSpan.FromSeconds(30),

            // ✨ Importante: tu claim de rol se llama "role"
            RoleClaimType = "role",
            // (opcional) si quieres fijar el "nombre" también:
            // NameClaimType = "sub",
        };
    });

// Autorización
builder.Services.AddAuthorization(opt =>
{
    opt.AddBookingAutoPolicies();
    opt.AddPolicy("Perm.Cash.View",    p => p.Requirements.Add(new PermissionRequirement(Permissions.Cash.View)));
    opt.AddPolicy("Perm.Cash.Manage",  p => p.Requirements.Add(new PermissionRequirement(Permissions.Cash.Manage)));
    opt.AddPolicy("Perm.Products.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Products.View)));
    opt.AddPolicy("Perm.Products.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Products.Manage)));
    opt.AddPolicy("Perm.Stock.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Stock.View)));
    opt.AddPolicy("Perm.Stock.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Stock.Manage)));
    opt.AddPolicy("Perm.Resources.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Resources.View)));
    opt.AddPolicy("Perm.Resources.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Resources.Manage)));
    opt.AddPolicy("Perm.Calendars.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Calendars.View)));
    opt.AddPolicy("Perm.Calendars.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Calendars.Manage)));
    opt.AddPolicy("Perm.Bookings.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Bookings.View)));
    opt.AddPolicy("Perm.Bookings.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Bookings.Manage)));
    opt.AddPolicy("Perm.Users.View",   p => p.Requirements.Add(new PermissionRequirement(Permissions.Users.View)));
    opt.AddPolicy("Perm.Users.Manage", p => p.Requirements.Add(new PermissionRequirement(Permissions.Users.Manage)));
});

// Handlers + helpers
builder.Services.AddSingleton<IAuthorizationHandler, AdminOrBranchAdminHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, BranchScopeHandler>();

// Servicios dominio
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
// Platform
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IPlatformAccountService, WebApi.Infrastructure.Services.Platform.PlatformAccountService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IPlatformTicketService, WebApi.Infrastructure.Services.Platform.PlatformTicketService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IPlatformSubscriptionService, WebApi.Infrastructure.Services.Platform.PlatformSubscriptionService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Platform.IImpersonationService, WebApi.Infrastructure.Services.Platform.ImpersonationService>();

// Notificación
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.INotificationConfigService, WebApi.Infrastructure.Services.Notification.NotificationConfigService>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.IEmailSender, WebApi.Infrastructure.Services.Notification.SmtpEmailSender>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.IPushSender, WebApi.Infrastructure.Services.Notification.NoopPushSender>();
builder.Services.AddScoped<WebApi.Infrastructure.Services.Notification.INotificationTemplateService, WebApi.Infrastructure.Services.Notification.NotificationTemplateService>();

// Hosted
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.BookingReminderScheduler>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.NotificationDispatcher>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.CashAutoCloseJob>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.HoldSlotSweeper>();
builder.Services.AddHostedService<WebApi.Infrastructure.HostedServices.SubscriptionBillingJob>();

// MVC + filtros
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelValidationFilter>();
});

// Swagger (con JWT y schemaIds seguros)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bookitauto API",
        Version = "v1",
        Description = "API para Bookitauto (Staff/Platform)"
    });

    // Evitar conflictos de nombres de modelos
    c.CustomSchemaIds(t => t.FullName!.Replace("+", "."));

    // Bearer en Authorize
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT en formato: Bearer {tu_token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<RequireBranchHeaderFilter>();

var app = builder.Build();

// Desarrollo: errores detallados
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// (Opcional mientras depuras) comenta si oculta detalles
// app.UseExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookitauto API v1");
    c.RoutePrefix = "swagger"; // /swagger
});

// HTTPS solo fuera de dev (para no liar puertos)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log de URLs al arrancar
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Services
        .GetRequiredService<IServer>()
        .Features
        .Get<IServerAddressesFeature>()?
        .Addresses ?? new List<string>();

    foreach (var address in addresses)
    {
        Console.WriteLine($"➡️  API base:     {address}/api");
        Console.WriteLine($"➡️  Swagger UI:   {address}/swagger");
        Console.WriteLine($"➡️  OpenAPI JSON: {address}/swagger/v1/swagger.json");
    }
});

app.Run();
