using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using AISupportTriage.Data;
using AISupportTriage.AI;
using AISupportTriage.Services;
using AISupportTriage.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Configure logging
builder.Services.AddLogging(b => b.AddConsole());

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("AISupportTriage")
        .AddConsoleExporter());

// Configure AI Options
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));

// Configure AI ChatClient with full middleware pipeline
builder.Services.AddChatClient(services =>
{
    var cache = services.GetRequiredService<IDistributedCache>();
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var options = services.GetRequiredService<IOptions<AiOptions>>().Value;

    // Create Ollama client using Microsoft.Extensions.AI.Ollama
    IChatClient ollamaClient = new OllamaChatClient(
        new Uri(options.Endpoint),
        options.ModelName);

    return ollamaClient
        .AsBuilder()
        .ConfigureOptions(opt => opt.Temperature = options.Temperature)
        .UseDistributedCache(cache)
        .UseFunctionInvocation()
        .UseLogging(loggerFactory)
        .UseOpenTelemetry(sourceName: "AISupportTriage")
        .Build(services);
});

// Register SupportFunctions for AI function calling
builder.Services.AddSingleton<SupportFunctions>();

// Register application services
builder.Services.AddScoped<IKnownIssueService, KnownIssueService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketTriageService, TicketTriageService>();

// Configure health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    await SeedData.SeedKnownIssuesAsync(context);
}

app.Run();
