using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.OpenApi.Models;
using MySailorApi.Clients;
using MySailorApi.Configuration;
using MySailorApi.Filters;
using MySailorApi.Models;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.Configure<WeatherDataConfig>(builder.Configuration.GetSection("WeatherDataApi"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Sailor API", Version = "v1" });
    options.OperationFilter<OptionalCitySwaggerParamFilter>();
});

builder.Services.AddHttpClient<WeatherForecastApiClient>(client =>
{
    var uri = new Uri("https://api.openweathermap.org/");

    client.BaseAddress = uri ?? throw new InvalidOperationException("The URI parameter is missing.");
});

builder.AddRedisDistributedCache("cache");

// Add hybrid cache
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024 * 2;
    options.MaximumKeyLength = 256;

    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(60),
        LocalCacheExpiration = TimeSpan.FromSeconds(5)
    };
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("RequestLogger");

    logger.LogInformation("** Handling request:** {Path}", context.Request.Path);
    await next();

    logger.LogInformation("** Finished handling request. **");
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/weatherforecast/{city}", async (
        string city,
        [FromQuery] int? requestId,
        HybridCache hybridCache,
        WeatherForecastApiClient httpClient,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        logger.LogInformation("** GET WeatherForecast - RequestId: {requestId} **", requestId);
        if (string.IsNullOrEmpty(city))
        {
            return Results.BadRequest("Missing city parameter.");
        }

        // Cache key (city) should be enum, in order to avoid denial of service issues
        var cityForecast = await hybridCache.GetOrCreateAsync($"cities:{city}", async (ct) =>
        {
            var endpoint = $"data/2.5/weather?q={city}";
            
            await Task.Delay(2000, cancellationToken);
            var cityForecast = await httpClient.GetDataAsync(endpoint, ct);
            
            return JsonConvert.DeserializeObject<WeatherForecast>(cityForecast);
        }, cancellationToken: cancellationToken);

        return Results.Ok(cityForecast);
    })
    .WithName("GetWeatherForecast")
    .Produces<List<WeatherForecast>>(StatusCodes.Status200OK);

app.MapDefaultEndpoints();
app.Run();
