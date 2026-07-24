using System.Reflection;
using System.Security.Claims;
using System.Text;
using CanchasSinteticas.Api.Auth;
using CanchasSinteticas.Api.BackgroundJobs;
using CanchasSinteticas.Api.Middleware;
using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Application.Services;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Notifications;
using CanchasSinteticas.Infrastructure.Payments;
using CanchasSinteticas.Infrastructure.Persistence;
using CanchasSinteticas.Infrastructure.Receipts;
using CanchasSinteticas.Infrastructure.Repositories;
using CanchasSinteticas.Infrastructure.Security;
using CanchasSinteticas.Infrastructure.Seed;
using CanchasSinteticas.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// --- Persistencia en memoria (reemplaza la BD por ahora) ---
builder.Services.AddSingleton<InMemoryDatabase>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IVenueRepository, InMemoryVenueRepository>();
builder.Services.AddSingleton<ICourtRepository, InMemoryCourtRepository>();
builder.Services.AddSingleton<IPriceRuleRepository, InMemoryPriceRuleRepository>();
builder.Services.AddSingleton<IBlackoutRepository, InMemoryBlackoutRepository>();
builder.Services.AddSingleton<IReservationRepository, InMemoryReservationRepository>();
builder.Services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
builder.Services.AddSingleton<IMatchRepository, InMemoryMatchRepository>();
builder.Services.AddSingleton<IProcessedWebhookEventRepository, InMemoryProcessedWebhookEventRepository>();
builder.Services.AddSingleton<IReceiptRepository, InMemoryReceiptRepository>();

// --- Servicios de infraestructura ---
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// --- Pagos (pasarela, webhook, notificaciones, expiración) ---
var paymentsOptions = builder.Configuration.GetSection(PaymentsOptions.SectionName).Get<PaymentsOptions>()
    ?? new PaymentsOptions();
builder.Services.AddSingleton(paymentsOptions);
builder.Services.AddSingleton(new PaymentSettings(paymentsOptions.ExpiryMinutes));
builder.Services.AddSingleton<WompiSignatureVerifier>();
builder.Services.AddSingleton<IPaymentWebhookVerifier, WompiWebhookVerifier>();
builder.Services.AddSingleton<IPaymentGatewayCredentialsResolver, PaymentGatewayCredentialsResolver>();
builder.Services.AddSingleton<InAppNotifier>();
builder.Services.AddSingleton<EmailNotifier>();
builder.Services.AddSingleton<WhatsAppSmsNotifier>();
builder.Services.AddSingleton<INotificationSender, CompositeNotificationSender>();
builder.Services.AddSingleton<IReceiptGenerator, QuestPdfReceiptGenerator>();
builder.Services.AddHttpClient<IPaymentGateway, WompiPaymentGateway>();
builder.Services.AddHostedService<PaymentExpirySweeper>();

// --- Casos de uso (servicios de aplicación) ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<VenueService>();
builder.Services.AddScoped<CourtService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<BlackoutService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PaymentWebhookService>();
builder.Services.AddScoped<PaymentExpiryService>();
builder.Services.AddScoped<MatchSettlementService>();
builder.Services.AddScoped<ReceiptService>();
builder.Services.AddScoped<VenuePaymentConfigService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<MatchService>();

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

// --- Autenticación JWT ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Canchas Sintéticas API",
        Version = "v1",
        Description = "Plataforma multi-tenant de reserva de canchas sintéticas: marketplace para clientes y panel de gestión para dueños.",
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT (sin el prefijo 'Bearer').",
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>(),
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// --- CORS ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// --- Seed de datos de demostración ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InMemoryDatabase>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var clock = scope.ServiceProvider.GetRequiredService<IClock>();
    DatabaseSeeder.Seed(db, hasher, clock);
}

app.UseMiddleware<DomainExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Canchas Sintéticas API v1");
    c.RoutePrefix = "swagger";
});
app.UseCors();

// Default Content-Type to application/json for clients/agents that omit it on write requests.
app.Use(async (ctx, next) =>
{
    if (string.IsNullOrEmpty(ctx.Request.ContentType)
        && (HttpMethods.IsPost(ctx.Request.Method)
            || HttpMethods.IsPut(ctx.Request.Method)
            || HttpMethods.IsPatch(ctx.Request.Method)))
    {
        ctx.Request.ContentType = "application/json; charset=utf-8";
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Punto de entrada expuesto para pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
