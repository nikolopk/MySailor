using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.OpenApi.Models;
using MySailorApi.Clients;
using MySailorApi.Filters;
using MySailorApi.Models;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

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

// Add hybrid cache
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024 * 2;
    options.MaximumKeyLength = 256;

    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(1),
        LocalCacheExpiration = TimeSpan.FromSeconds(1)
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapGet("/weatherforecast/{city}", async (
        string city,
        HybridCache hybridCache,
        WeatherForecastApiClient httpClient,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrEmpty(city))
        {
            return Results.BadRequest("Missing city parameter.");
        }

        var cityForecast = await hybridCache.GetOrCreateAsync($"cities:{city}", async (ct) =>
        {
            var endpoint = $"data/2.5/weather?q={city}";
            var cityForecast = await httpClient.GetDataAsync(endpoint, ct);
            
            return JsonConvert.DeserializeObject<WeatherForecast>(cityForecast);
            //return cityForecast;
        }, cancellationToken: cancellationToken);

        return Results.Ok(cityForecast);
    })
    .WithName("GetWeatherForecast")
    .Produces<List<WeatherForecast>>(StatusCodes.Status200OK);

app.Run();
