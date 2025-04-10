using System;
using System.Collections.Concurrent;
using Bablomet.API.Indicators;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;

namespace Bablomet.API.Calculators;

public class ExponentialMovingAveragesCalculator
{
    private readonly int _period;
    private readonly decimal _smoothing;
    private readonly ConcurrentDictionary<BarKey, decimal> _emas;

    public ExponentialMovingAveragesCalculator(int period)
    {
        if (period <= 0 || period > 1000)
        {
            throw new ArgumentException("Unsupported value of SMA period");
        }

        _period = period;
        _smoothing = 2m / (_period + 1);

        _emas = new ConcurrentDictionary<BarKey, decimal>();
    }

    public ExponentialMovingAverage Calculate(Bar bar)
    {
        if (bar == null) throw new ArgumentNullException(nameof(bar));

        var key = new BarKey(bar.Symbol, bar.TimeFrame);
        if (!_emas.TryGetValue(key, out var ema))
        {
            _emas[key] = ema = bar.Close;
            return new ExponentialMovingAverage
            {
                Symbol = bar.Symbol,
                TimeFrame = bar.TimeFrame,
                Time = bar.Time,
                Value = ema
            };
        }
        
        ema = (bar.Close * _smoothing) + (ema * (1 - _smoothing));
        return new ExponentialMovingAverage
        {
            Symbol = bar.Symbol,
            TimeFrame = bar.TimeFrame,
            Time = bar.Time,
            Value = ema
        };
    }
}