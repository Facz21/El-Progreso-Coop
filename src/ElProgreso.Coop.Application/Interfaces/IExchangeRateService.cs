using ElProgreso.Coop.Application.DTOs;

namespace ElProgreso.Coop.Application.Interfaces;

public interface IExchangeRateService
{
    Task<ExchangeRateResult> GetUsdExchangeRateAsync();
}
