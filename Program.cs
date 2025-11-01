using FVTIntegrationMiddleware.Configuration;
using FVTIntegrationMiddleware.Middleware;
using FVTIntegrationMiddleware.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/fvt-middleware-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure proxy settings from appsettings.json
builder.Services.Configure<ProxyConfiguration>(
    builder.Configuration.GetSection("ProxyConfiguration"));

// Register custom services
builder.Services.AddSingleton<ITransformationService, TransformationService>();
builder.Services.AddSingleton<IProxyRuleEngine, ProxyRuleEngine>();

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom middleware for request/response interception
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RequestTransformationMiddleware>();
app.UseMiddleware<ResponseTransformationMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Map reverse proxy routes
app.MapReverseProxy();

Log.Information("FVT Integration Middleware starting...");

app.Run();

Log.CloseAndFlush();
