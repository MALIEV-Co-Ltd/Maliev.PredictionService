using System.Threading.RateLimiting;
using Maliev.Aspire.ServiceDefaults;
using Maliev.PredictionService.Api.Extensions;
using Maliev.PredictionService.Infrastructure.Storage;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience

// --- MassTransit with RabbitMQ ---
builder.AddMassTransitWithRabbitMq(x =>
{
    x.AddConsumers(typeof(Maliev.PredictionService.Infrastructure.AssemblyReference).Assembly);
});

// --- Model Storage (must be before AddPredictionService) ---
builder.AddModelStorage(); // Automatic JWT auth via ServiceAccountAuthenticationHandler

// --- PredictionService Dependencies ---
builder.Services.AddPredictionService(builder.Configuration);

// --- API Infrastructure ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- Authentication & Authorization ---
// JWT Authentication with permission-based authorization (AddJwtAuthentication includes AddPermissionAuthorization)
builder.AddJwtAuthentication();

// --- Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    // Sliding window policy for prediction endpoints
    options.AddPolicy("predictions", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100, // 100 requests
                Window = TimeSpan.FromMinutes(1), // per minute
                SegmentsPerWindow = 6, // Check every 10 seconds
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10 // Allow 10 queued requests
            }));

    // Stricter policy for model training endpoints
    options.AddPolicy("training", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, // 10 requests
                Window = TimeSpan.FromHours(1), // per hour
                QueueLimit = 2
            }));

    // Global fallback policy
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// --- API Configuration ---
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader (FR-051)

// CORS (if needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// --- Middleware Pipeline ---

// Map health check and metrics endpoints via ServiceDefaults
app.MapDefaultEndpoints("predictionservice");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Rate Limiting
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
