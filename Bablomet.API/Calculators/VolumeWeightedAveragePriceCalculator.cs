using System;
using System.Collections.Concurrent;
using Bablomet.API.Indicators;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;

namespace Bablomet.API.Calculators;

public class VolumeWeightedAveragePriceCalculator
{
    private readonly ConcurrentDictionary<BarKey, VwapInfo> _vwaps = new();
    
    public VolumeWeightedAveragePrice Calculate(Bar bar)
    {
        if (bar == null) throw new ArgumentNullException(nameof(bar));

        var key = new BarKey(bar.Symbol, bar.TimeFrame);

        if (!_vwaps.TryGetValue(key, out var info))
        {
            _vwaps[key] = info = new VwapInfo();
        }

        var typicalPrice = (bar.High + bar.Low + bar.Close) / 3;
        info.Numerator += typicalPrice * bar.Volume;
        info.TotalVolume += bar.Volume;

        return new VolumeWeightedAveragePrice
        {
            Symbol = bar.Symbol,
            TimeFrame = bar.TimeFrame,
            Time = bar.Time,
            Value = info.TotalVolume != 0 ? info.Numerator / info.TotalVolume : 0
        };
    }

    private class VwapInfo
    {
        public decimal TotalVolume { get; set; }
        public decimal Numerator { get; set; }
    }
}