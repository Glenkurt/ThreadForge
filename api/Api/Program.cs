using Api.Data;
using Api.Extensions;
using Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

// Configure Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting application...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings
    builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "ThreadForge")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName),
        preserveStaticLogger: true);

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ThreadForge API",
            Version = "v1",
            Description = "AI-powered Twitter thread generation for indie hackers",
            Contact = new OpenApiContact
            {
                Name = "ThreadForge Support",
                Email = "support@threadforge.dev"
            }
        });

        // Include XML comments
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // Database
    if (builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));
    }
    else
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    }

    // Application services (business logic layer)
    builder.Services.AddApplicationServices();

    // Grok (xAI)
    builder.Services.AddXai(builder.Configuration);

    // JWT Authentication
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();

    // Rate limiting
    builder.Services.AddRateLimiting();

    // CORS - Restrict to specific methods and headers for security
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:4200"])
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization", "X-Client-Id", "X-Gateway-Token")
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // Request logging with Serilog
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    // Global exception handling (first in pipeline)
    app.UseGlobalExceptionHandling();

    // Gateway protection (requires password to access the site)
    app.UseGatewayProtection();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ThreadForge API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseCors("AllowFrontend");

    // Rate limiting
    app.UseRateLimiter();

    // Serve static files (Angular app in production)
    app.UseDefaultFiles();
    app.UseStaticFiles();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Fallback to index.html for Angular routing
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for WebApplicationFactory in integration tests
public partial class Program { }
