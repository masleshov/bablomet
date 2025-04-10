using System.Collections.Concurrent;
using System.Collections.Generic;
using Bablomet.API.Indicators;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;

namespace Bablomet.API.Calculators;


public class MacdCalculator
{
    private readonly ExponentialMovingAveragesCalculator _shortTermEmaCalculator;
    private readonly ExponentialMovingAveragesCalculator _longTermEmaCalculator;
    private readonly ExponentialMovingAveragesCalculator _signalLineCalculator;

    private readonly ConcurrentDictionary<BarKey, Queue<decimal>> _macdLineValues = new();
    private readonly int _signalLinePeriod;

    public MacdCalculator(int shortTermPeriod = 12, int longTermPeriod = 26, int signalLinePeriod = 9)
    {
        _shortTermEmaCalculator = new ExponentialMovingAveragesCalculator(shortTermPeriod);
        _longTermEmaCalculator = new ExponentialMovingAveragesCalculator(longTermPeriod);
        _signalLineCalculator = new ExponentialMovingAveragesCalculator(signalLinePeriod);

        _signalLinePeriod = signalLinePeriod;
    }

    public MovingAverageConvergenceDivergence Calculate(Bar bar)
    {
        var shortTermEma = _shortTermEmaCalculator.Calculate(bar).Value;
        var longTermEma = _longTermEmaCalculator.Calculate(bar).Value;

        var macdLine = shortTermEma - longTermEma;

        // Update the queue for the signal line calculation
        var key = new BarKey(bar.Symbol, bar.TimeFrame);
        if (!_macdLineValues.TryGetValue(key, out var queue))
        {
            _macdLineValues[key] = queue = new Queue<decimal>();
        }
        
        queue.Enqueue(macdLine);
        if (queue.Count > _signalLinePeriod)
        {
            queue.Dequeue();
        }

        // Create a temporary Bar object for signal line calculation
        var signalLineBar = new Bar
        {
            Symbol = bar.Symbol,
            TimeFrame = bar.TimeFrame,
            Time = bar.Time,
            Close = queue.Peek() // Use the latest MACD line value as the 'close' for calculation
        };

        var signalLine = _signalLineCalculator.Calculate(signalLineBar).Value;
        var histogram = macdLine - signalLine;

        return new MovingAverageConvergenceDivergence
        {
            Symbol = bar.Symbol,
            TimeFrame = bar.TimeFrame,
            Time = bar.Time,
            MacdLine = macdLine,
            SignalLine = signalLine,
            Histogram = histogram
        };
    }
}