using Microsoft.Extensions.Logging;
using Bablomet.Common.Infrastructure;
using Bablomet.Common.Web;
using Bablomet.PRO.Telegram.Infrastructure;

namespace Bablomet.PRO.Telegram.Services;

public class SignalSettingsService
{
    private readonly IIndicatorsApi _indicatorsApi;
    private readonly ILogger<SignalSettingsService> _logger;

    private static readonly Dictionary<IndicatorType, string[]> _defaultSettings = new()
    {
        { IndicatorType.SMA, new[] { "50", "200" } },
        { IndicatorType.EMA, new[] { "21", "50" } },
        { IndicatorType.MACD, new[] { "12", "26", "9" } },
        { IndicatorType.VWAP, Array.Empty<string>() }
    };

    public SignalSettingsService(IIndicatorsApi indicatorsApi, ILogger<SignalSettingsService> logger)
    {
        _indicatorsApi = indicatorsApi ?? throw new ArgumentNullException(nameof(indicatorsApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string[] GetDefaultParameters(IndicatorType signalType)
    {
        return _defaultSettings.TryGetValue(signalType, out var parameters)
            ? parameters
            : Array.Empty<string>();
    }

    public bool ValidateUserParameters(IndicatorType signalType, string parametersInput)
    {
        if (string.IsNullOrWhiteSpace(parametersInput))
            return false;

        var userParams = parametersInput.Split(',').Select(p => p.Trim()).ToArray();

        return signalType switch
        {
            IndicatorType.SMA or IndicatorType.EMA =>
                userParams.Length == 2 && userParams.All(IsPositiveInteger),

            IndicatorType.MACD =>
                userParams.Length == 3 && userParams.All(IsPositiveInteger),

            IndicatorType.VWAP => true,

            _ => false
        };
    }

    private static bool IsPositiveInteger(string value)
    {
        return int.TryParse(value, out var number) && number > 0;
    }

    public async Task SubscribeUserToIndicatorAsync(long chatId, IndicatorType indicatorType, string symbol, string timeFrame, int[] parameters)
    {
        var subscription = new IndicatorSubscription(indicatorType, symbol, timeFrame, parameters);

        try
        {
            IndicatorSubscriptionsCache.Subscribe(subscription, chatId);
            await _indicatorsApi.SubscribeAsync(subscription);
            _logger.LogInformation(
                "User {ChatId} subscribed to {IndicatorType} signals for {Symbol} [{TimeFrame}] with params {Parameters}",
                chatId, indicatorType, symbol, timeFrame, string.Join(",", parameters));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed subscription: user {ChatId}, indicator {IndicatorType}, {Symbol} [{TimeFrame}]",
                chatId, indicatorType, symbol, timeFrame);
            throw;
        }
    }
}
