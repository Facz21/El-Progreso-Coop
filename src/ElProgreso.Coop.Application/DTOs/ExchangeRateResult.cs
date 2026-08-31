namespace ElProgreso.Coop.Application.DTOs;

public class ExchangeRateResult
{
    public bool IsSuccess { get; set; }
    public decimal Rate { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? ErrorMessage { get; set; }

    public static ExchangeRateResult Success(decimal rate, DateTime? validFrom, DateTime? validTo) =>
        new()
        {
            IsSuccess = true,
            Rate = rate,
            ValidFrom = validFrom,
            ValidTo = validTo
        };

    public static ExchangeRateResult Failure(string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}
