namespace MySailorApi.Clients;

public class WeatherForecastApiClient
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "fbdbdb3a4e4d9342217b50c2b656df63";

    public WeatherForecastApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetDataAsync(string endpoint, CancellationToken ct = default)
    {
        var url = $"{endpoint}&appid={ApiKey}";
        var response = await _httpClient.GetAsync(url, ct);

        return await response.Content.ReadAsStringAsync(ct);
    }
}