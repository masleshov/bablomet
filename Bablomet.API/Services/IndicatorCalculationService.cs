using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bablomet.API.Calculators;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
using Bablomet.Common.Web;
using Microsoft.Extensions.Logging;

namespace Bablomet.API.Infrastructure;

public class IndicatorCalculationService
{
    private readonly IndicatorPublisher _publisher;
    private readonly ILogger<IndicatorCalculationService> _logger;

    private readonly ConcurrentDictionary<IndicatorKey, SimpleMovingAveragesCalculator> _smaCalculators = new();
    private readonly ConcurrentDictionary<IndicatorKey, ExponentialMovingAveragesCalculator> _emaCalculators = new();
    private readonly ConcurrentDictionary<MacdKey, MacdCalculator> _macdCalculators = new();
    private readonly ConcurrentDictionary<BarKey, VolumeWeightedAveragePriceCalculator> _vwapCalculators = new();

    private readonly ConcurrentDictionary<IndicatorSubscription, IndicatorValues> _previousIndicatorValues = new();

    public IndicatorCalculationService(
        IndicatorPublisher publisher,
        ILogger<IndicatorCalculationService> logger)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessBarAsync(Bar bar)
    {
        var subscriptions = IndicatorSubscriptionsCache.GetSubscriptionsForBar(new BarKey(bar.Symbol, bar.TimeFrame));

        foreach (var subscription in subscriptions)
        {
            switch (subscription.IndicatorType)
            {
                case IndicatorType.SMA when subscription.Parameters.Length == 2:
                    await ProcessCrossoverAsync(bar, subscription, CalculateSma);
                    break;

                case IndicatorType.EMA when subscription.Parameters.Length == 2:
                    await ProcessCrossoverAsync(bar, subscription, CalculateEma);
                    break;

                case IndicatorType.MACD when subscription.Parameters.Length == 3:
                    await ProcessMacdSignalAsync(bar, subscription);
                    break;

                case IndicatorType.VWAP:
                    await ProcessVwapSignalAsync(bar, subscription);
                    break;

                default:
                    _logger.LogWarning("Unsupported or invalid subscription: {IndicatorType}, {Symbol}, {TimeFrame}, {Parameters}",
                        subscription.IndicatorType, bar.Symbol, bar.TimeFrame, string.Join(",", subscription.Parameters));
                    continue;
            }
        }
    }

    private async Task ProcessCrossoverAsync(Bar bar, IndicatorSubscription subscription, Func<Bar, int, decimal> calculate)
    {
        var shortPeriod = subscription.Parameters.Min();
        var longPeriod = subscription.Parameters.Max();

        var shortValue = calculate(bar, shortPeriod);
        var longValue = calculate(bar, longPeriod);

        var key = new IndicatorSubscription(subscription.IndicatorType, bar.Symbol, bar.TimeFrame, subscription.Parameters);
        var prev = _previousIndicatorValues.GetOrAdd(key, _ => new IndicatorValues());

        if (prev.Short.HasValue && prev.Long.HasValue)
        {
            var signal = GetCrossoverSignal(prev.Short.Value, prev.Long.Value, shortValue, longValue);
            if (signal != CrossoverSignal.None)
            {
                await _publisher.PublishIndicatorAsync(new IndicatorSignal
                {
                    IndicatorType = subscription.IndicatorType,
                    Parameters = subscription.Parameters,
                    Symbol = bar.Symbol,
                    TimeFrame = bar.TimeFrame,
                    Time = bar.Time,
                    Signal = signal
                });

                _logger.LogInformation("{IndicatorType} {Signal} crossover for {Symbol} [{TimeFrame}] {ShortPeriod}({Short}) crossed {LongPeriod}({Long})",
                    subscription.IndicatorType, signal, bar.Symbol, bar.TimeFrame, shortPeriod, shortValue, longPeriod, longValue);
            }
        }

        prev.Short = shortValue;
        prev.Long = longValue;
    }

    private async Task ProcessMacdSignalAsync(Bar bar, IndicatorSubscription subscription)
    {
        var (shortPeriod, longPeriod, signalPeriod) = (subscription.Parameters[0], subscription.Parameters[1], subscription.Parameters[2]);
        var key = new MacdKey(bar.Symbol, bar.TimeFrame, shortPeriod, longPeriod, signalPeriod);

        var calculator = _macdCalculators.GetOrAdd(key, _ => new MacdCalculator(shortPeriod, longPeriod, signalPeriod));
        var macd = calculator.Calculate(bar);

        var subKey = new IndicatorSubscription(subscription.IndicatorType, bar.Symbol, bar.TimeFrame, subscription.Parameters);
        var prev = _previousIndicatorValues.GetOrAdd(subKey, _ => new IndicatorValues());

        if (prev.Short.HasValue && prev.Long.HasValue)
        {
            var signal = GetCrossoverSignal(prev.Short.Value, prev.Long.Value, macd.MacdLine, macd.SignalLine);
            if (signal != CrossoverSignal.None)
            {
                await _publisher.PublishIndicatorAsync(new IndicatorSignal
                {
                    IndicatorType = subscription.IndicatorType,
                    Parameters = subscription.Parameters,
                    Symbol = bar.Symbol,
                    TimeFrame = bar.TimeFrame,
                    Time = bar.Time,
                    Signal = signal
                });

                _logger.LogInformation("MACD {Signal} crossover for {Symbol} [{TimeFrame}] MACD({MacdLine}) crossed Signal({SignalLine})",
                    signal, bar.Symbol, bar.TimeFrame, macd.MacdLine, macd.SignalLine);
            }
        }

        prev.Short = macd.MacdLine;
        prev.Long = macd.SignalLine;
    }

    private async Task ProcessVwapSignalAsync(Bar bar, IndicatorSubscription subscription)
    {
        var key = new BarKey(bar.Symbol, bar.TimeFrame);
        var calculator = _vwapCalculators.GetOrAdd(key, _ => new VolumeWeightedAveragePriceCalculator());

        var vwap = calculator.Calculate(bar);

        var subKey = new IndicatorSubscription(subscription.IndicatorType, bar.Symbol, bar.TimeFrame, subscription.Parameters);
        var prev = _previousIndicatorValues.GetOrAdd(subKey, _ => new IndicatorValues());

        if (prev.Short.HasValue)
        {
            var signal = GetCrossoverSignal(prev.Short.Value, vwap.Value, bar.Close, vwap.Value);
            if (signal != CrossoverSignal.None)
            {
                await _publisher.PublishIndicatorAsync(new IndicatorSignal
                {
                    IndicatorType = subscription.IndicatorType,
                    Parameters = subscription.Parameters,
                    Symbol = bar.Symbol,
                    TimeFrame = bar.TimeFrame,
                    Time = bar.Time,
                    Signal = signal
                });

                _logger.LogInformation("VWAP {Signal} crossover for {Symbol} [{TimeFrame}] Price({Price}) crossed VWAP({Vwap})",
                    signal, bar.Symbol, bar.TimeFrame, bar.Close, vwap.Value);
            }
        }

        prev.Short = bar.Close;
        prev.Long = vwap.Value;
    }

    private decimal CalculateSma(Bar bar, int period)
    {
        var key = new IndicatorKey(bar.Symbol, bar.TimeFrame, period);
        var calculator = _smaCalculators.GetOrAdd(key, _ => new SimpleMovingAveragesCalculator(period));
        return calculator.Calculate(bar).Value;
    }

    private decimal CalculateEma(Bar bar, int period)
    {
        var key = new IndicatorKey(bar.Symbol, bar.TimeFrame, period);
        var calculator = _emaCalculators.GetOrAdd(key, _ => new ExponentialMovingAveragesCalculator(period));
        return calculator.Calculate(bar).Value;
    }

    private CrossoverSignal GetCrossoverSignal(decimal prevShort, decimal prevLong, decimal currentShort, decimal currentLong)
    {
        if (prevShort <= prevLong && currentShort > currentLong)
            return CrossoverSignal.Bullish;

        if (prevShort >= prevLong && currentShort < currentLong)
            return CrossoverSignal.Bearish;

        return CrossoverSignal.None;
    }

    private class IndicatorValues
    {
        public decimal? Short { get; set; }
        public decimal? Long { get; set; }
    }

    private record IndicatorKey(string Symbol, string TimeFrame, int Period);
    private record MacdKey(string Symbol, string TimeFrame, int ShortPeriod, int LongPeriod, int SignalPeriod);
}
