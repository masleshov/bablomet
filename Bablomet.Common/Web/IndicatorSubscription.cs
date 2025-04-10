using System;
using System.Linq;
using Bablomet.Common.Infrastructure;

namespace Bablomet.Common.Web;

public sealed class IndicatorSubscription
{
    public IndicatorType IndicatorType { get; private set; } 
    public string Symbol { get; private set; }
    public string TimeFrame { get; private set; }
    public int[] Parameters { get; private set; }

    public IndicatorSubscription(IndicatorType indicatorType, string symbol, string timeFrame, int[] parameters)
    {
        IndicatorType = indicatorType;
        Symbol = symbol;
        TimeFrame = timeFrame;
        Parameters = parameters;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IndicatorSubscription other)
            return false;

        return IndicatorType == other.IndicatorType &&
               Symbol == other.Symbol &&
               TimeFrame == other.TimeFrame &&
               Parameters.SequenceEqual(other.Parameters);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IndicatorType);
        hash.Add(Symbol);
        hash.Add(TimeFrame);
        foreach (var param in Parameters)
        {
            hash.Add(param);
        }
        return hash.ToHashCode();
    }
}