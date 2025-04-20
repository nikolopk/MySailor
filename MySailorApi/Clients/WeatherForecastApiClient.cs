using Microsoft.Extensions.Options;
using MySailorApi.Configuration;

namespace MySailorApi.Clients;

public class WeatherForecastApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public WeatherForecastApiClient(HttpClient httpClient, IOptions<WeatherDataConfig> config)
    {
        _httpClient = httpClient;

        _apiKey = config.Value?.Key ?? string.Empty;
    }

    public async Task<string> GetDataAsync(string endpoint, CancellationToken ct = default)
    {
        var url = $"{endpoint}&appid={_apiKey}";
        var response = await _httpClient.GetAsync(url, ct);

        return await response.Content.ReadAsStringAsync(ct);
    }
}