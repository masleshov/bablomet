using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bablomet.API.Calculators;
using Bablomet.API.Domain;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;

namespace Bablomet.API.Strategies;

public class MovingAverageCrossoverStrategy
{
    private readonly string _portfolioCode;
    private readonly int _maxTradeAmount;
    private readonly string _timeFrame;
    private readonly Dictionary<BarKey, PreviousValues> _previous;

    private readonly int _adxThreshold;
    
    private readonly SimpleMovingAveragesCalculator _smaCalculator;
    private readonly ExponentialMovingAveragesCalculator _emaCalculator;
    private readonly VolumeWeightedAveragePriceCalculator _vwapCalculator;
    private readonly MacdCalculator _macdCalculator;
    private readonly AverageDirectionalIndexCalculator _adxCalculator;

    public readonly string StrategyCode;

    public MovingAverageCrossoverStrategy(string strategyCode, string portfolioCode, MovingAverageCrossoverStrategyOptions options)
    {
        if (string.IsNullOrWhiteSpace(strategyCode)) throw new ArgumentNullException(nameof(strategyCode));
        if (string.IsNullOrWhiteSpace(portfolioCode)) throw new ArgumentNullException(nameof(portfolioCode));
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (options.MaxTradeAmount <= 0) throw new ArgumentNullException(nameof(options.MaxTradeAmount));
        if (string.IsNullOrWhiteSpace(options.TimeFrame)) throw new ArgumentNullException(nameof(options.TimeFrame));
        if (options.SmaPeriod <= 0) throw new ArgumentNullException(nameof(options.SmaPeriod));
        if (options.EmaPeriod <= 0) throw new ArgumentNullException(nameof(options.EmaPeriod));
        if (options.MacdShortPeriod <= 0) throw new ArgumentNullException(nameof(options.MacdShortPeriod));
        if (options.MacdLongPeriod <= 0) throw new ArgumentNullException(nameof(options.MacdLongPeriod));
        if (options.MacdSignalLinePeriod <= 0) throw new ArgumentNullException(nameof(options.MacdSignalLinePeriod));

        StrategyCode = strategyCode;

        _portfolioCode = portfolioCode;
        _maxTradeAmount = options.MaxTradeAmount;
        _timeFrame = options.TimeFrame;
        
        _smaCalculator = new SimpleMovingAveragesCalculator(options.SmaPeriod);
        _emaCalculator = new ExponentialMovingAveragesCalculator(options.EmaPeriod);
        _vwapCalculator = new VolumeWeightedAveragePriceCalculator();
        _macdCalculator = new MacdCalculator(options.MacdShortPeriod, options.MacdLongPeriod, options.MacdSignalLinePeriod);
        _adxCalculator = new AverageDirectionalIndexCalculator(options.AdxPeriod);

        _adxThreshold = options.AdxThreshold;

        _previous = new Dictionary<BarKey, PreviousValues>();
    }

    public async Task ProcessBar(Bar bar, Func<Signal, Task> onSignal)
    {
        if (bar == null) throw new ArgumentNullException(nameof(bar));
        if (bar.TimeFrame != _timeFrame) return;

        var sma = _smaCalculator.Calculate(bar);
        var ema = _emaCalculator.Calculate(bar);
        var vwap = _vwapCalculator.Calculate(bar);
        var macd = _macdCalculator.Calculate(bar);
        var adx = _adxCalculator.Calculate(bar);

        // Ensure that ADX is available and indicates a strong trend
        // if (!adx.HasValue || adx < _adxThreshold)
        // {
        //     Console.WriteLine($"Weak trend, ADX = {adx.Value}");
        //     return; // Skip trades if ADX indicates a weak trend (below threshold)
        // }
        
        var key = new BarKey(bar.Symbol, bar.TimeFrame);
        var instrument = InstrumentsCache.GetInstrument(bar.Symbol);
        if (instrument == null)
        {
            Console.WriteLine($"No instrument {bar.Symbol} found in the cache");
            return;
        }
        
        if (!_previous.TryGetValue(key, out var previousValues))
        {
            _previous[key] = new PreviousValues
            {
                PreviousSma = sma.Value,
                PreviousEma = ema.Value,
                PreviousMacdLine = macd.MacdLine,
                PreviousSignalLine = macd.SignalLine
            };
            return;
        }
        
        var buySignal = previousValues.PreviousEma < previousValues.PreviousSma && ema.Value > sma.Value &&
                        previousValues.PreviousMacdLine < previousValues.PreviousSignalLine &&
                        macd.MacdLine > macd.SignalLine &&
                        bar.Close > vwap.Value;

        var sellSignal = previousValues.PreviousEma > previousValues.PreviousSma && ema.Value < sma.Value &&
                         previousValues.PreviousMacdLine > previousValues.PreviousSignalLine &&
                         macd.MacdLine < macd.SignalLine &&
                         bar.Close < vwap.Value;

        if (buySignal)
        {
            if (PositionsCache.GetMoney(_portfolioCode, instrument.Currency) < _maxTradeAmount)
            {
                return;
            }

            var quantity = Convert.ToInt32(Math.Floor(_maxTradeAmount / bar.Close));
            if (quantity == 0)
            {
                return;
            }

            await onSignal(new Signal
            {
                Direction = MoveDirection.Buy,
                PortfolioCode = _portfolioCode,
                Instrument = instrument,
                Quantity = quantity,
                Price = bar.Close
            });
        }
        else if (sellSignal)
        {
            var position = PositionsCache.GetPosition(_portfolioCode, instrument.Symbol);
            if (position == null)
            {
                return;
            }

            await onSignal(new Signal
            {
                Direction = MoveDirection.Sell,
                PortfolioCode = _portfolioCode,
                Instrument = instrument,
                Quantity = position.TotalQuantity,
                Price = bar.Close
            });
        }

        _previous[key] = new PreviousValues
        {
            PreviousSma = sma.Value,
            PreviousEma = ema.Value,
            PreviousMacdLine = macd.MacdLine,
            PreviousSignalLine = macd.SignalLine
        };
    }

    private class PreviousValues
    {
        public decimal PreviousSma { get; set; }
        public decimal PreviousEma { get; set; }
        public decimal PreviousMacdLine { get; set; }
        public decimal PreviousSignalLine { get; set; }
    }
}