using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElProgreso.Coop.Application.DTOs;
using ElProgreso.Coop.Application.Interfaces;

namespace ElProgreso.Coop.Infrastructure.Services;

/// <summary>
/// Consumes the official Colombian government open data API (datos.gov.co) for the USD TRM exchange rate.
/// Includes graceful failure fallback mechanism to ensure teller terminal availability.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private const string ApiUrl = "https://datos.gov.co/resource/32sa-8pi3.json?$order=vigenciadesde%20DESC&$limit=1";
    private readonly HttpClient _httpClient;

    public ExchangeRateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    /// <summary>
    /// Asynchronously fetches the latest official TRM rate and its validity period.
    /// </summary>
    /// <returns>A result object containing the exchange rate or error details on network failure.</returns>
    public async Task<ExchangeRateResult> GetUsdExchangeRateAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync(ApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return ExchangeRateResult.Failure($"HTTP error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var records = JsonSerializer.Deserialize<List<TrmApiResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (records == null || records.Count == 0)
            {
                return ExchangeRateResult.Failure("No exchange rate records found in the API response.");
            }

            var latest = records[0];

            if (!decimal.TryParse(latest.Valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            {
                return ExchangeRateResult.Failure($"Could not parse rate value: '{latest.Valor}'");
            }

            DateTime? validFrom = DateTime.TryParse(latest.VigenciaDesde, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ? from : null;
            DateTime? validTo = DateTime.TryParse(latest.VigenciaHasta, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to) ? to : null;

            return ExchangeRateResult.Success(rate, validFrom, validTo);
        }
        catch (HttpRequestException ex)
        {
            return ExchangeRateResult.Failure($"Network error while fetching USD exchange rate: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ExchangeRateResult.Failure("Request timed out while fetching USD exchange rate.");
        }
        catch (Exception ex)
        {
            return ExchangeRateResult.Failure($"Unexpected error while fetching USD exchange rate: {ex.Message}");
        }
    }

    private class TrmApiResponse
    {
        [JsonPropertyName("valor")]
        public string? Valor { get; set; }

        [JsonPropertyName("unidad")]
        public string? Unidad { get; set; }

        [JsonPropertyName("vigenciadesde")]
        public string? VigenciaDesde { get; set; }

        [JsonPropertyName("vigenciahasta")]
        public string? VigenciaHasta { get; set; }
    }
}
