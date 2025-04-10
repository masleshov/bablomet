using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bablomet.API.Indicators;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;

namespace Bablomet.API.Calculators;

public class SimpleMovingAveragesCalculator
{
    private readonly int _period;
    private readonly ConcurrentDictionary<BarKey, Queue<decimal>> _closingPrices;

    public SimpleMovingAveragesCalculator(int period)
    {
        if (period <= 0 || period > 1000)
        {
            throw new ArgumentException("Unsupported value of SMA period");
        }

        _period = period;
        _closingPrices = new ConcurrentDictionary<BarKey, Queue<decimal>>();
    }

    public SimpleMovingAverage Calculate(Bar bar)
    {
        if (bar == null) throw new ArgumentNullException(nameof(bar));

        var key = new BarKey(bar.Symbol, bar.TimeFrame);
        if (!_closingPrices.TryGetValue(key, out var queue))
        {
            _closingPrices[key] = queue = new Queue<decimal>(_period);
        }
        
        if (queue.Count == _period)
        {
            queue.Dequeue();
        }

        queue.Enqueue(bar.Close);
        return new SimpleMovingAverage
        {
            Symbol = bar.Symbol,
            TimeFrame = bar.TimeFrame,
            Time = bar.Time,
            Value = queue.Average()
        };
    }
}