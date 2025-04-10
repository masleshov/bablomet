using System;

namespace Bablomet.API.Infrastructure;

public readonly struct BarKey
{
    public readonly string Symbol;
    public readonly string TimeFrame;

    public BarKey(string symbol, string timeFrame)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentNullException(nameof(symbol));
        if (string.IsNullOrWhiteSpace(timeFrame)) throw new ArgumentNullException(nameof(timeFrame));

        Symbol = symbol;
        TimeFrame = timeFrame;
    }
}