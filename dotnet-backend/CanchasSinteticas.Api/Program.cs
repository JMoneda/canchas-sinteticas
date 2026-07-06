using System.Reflection;
using CanchasSinteticas.Api.Middleware;
using CanchasSinteticas.Application.UseCases;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Data;
using CanchasSinteticas.Infrastructure.Repositories;
using CanchasSinteticas.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=reservations.db";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(dbPath));

builder.Services.AddScoped<IFieldRepository, SqliteFieldRepository>();
builder.Services.AddScoped<IReservationRepository, SqliteReservationRepository>();
builder.Services.AddScoped<ListAvailableSlotsUseCase>();
builder.Services.AddScoped<CreateReservationUseCase>();
builder.Services.AddScoped<ListReservationsUseCase>();
builder.Services.AddScoped<CancelReservationUseCase>();

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Canchas Sintéticas API",
        Version = "v1",
        Description = "API de reservas de canchas de fútbol sintético. " +
                      "Permite consultar disponibilidad, crear y cancelar reservas.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DatabaseSeeder.Seed(db);
}

app.UseMiddleware<DomainExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Canchas Sintéticas API v1");
    c.RoutePrefix = "swagger";
});
app.UseCors();

// Default Content-Type to application/json for agents (e.g. Copilot Studio) that omit it.
// Reads and stores raw body text in HttpContext.Items before model binding consumes the stream.
app.Use(async (ctx, next) =>
{
    if (string.IsNullOrEmpty(ctx.Request.ContentType)
        && (HttpMethods.IsPost(ctx.Request.Method)
            || HttpMethods.IsPut(ctx.Request.Method)
            || HttpMethods.IsPatch(ctx.Request.Method)))
    {
        ctx.Request.ContentType = "application/json; charset=utf-8";
    }

    ctx.Request.EnableBuffering();
    using (var reader = new StreamReader(ctx.Request.Body, leaveOpen: true))
        ctx.Items["RawRequestBody"] = await reader.ReadToEndAsync();
    if (ctx.Request.Body.CanSeek)
        ctx.Request.Body.Position = 0;

    await next();
});

app.MapControllers();

app.Run();
