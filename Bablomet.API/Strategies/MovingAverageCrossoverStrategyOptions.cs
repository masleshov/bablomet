namespace Bablomet.API.Strategies;

public class MovingAverageCrossoverStrategyOptions
{
    public int MaxTradeAmount { get; init; }
    public string TimeFrame { get; init; }
    public int SmaPeriod { get; init; }
    public int EmaPeriod { get; init; }
    public int MacdShortPeriod { get; init; }
    public int MacdLongPeriod { get; init; }
    public int MacdSignalLinePeriod { get; init; }
    public int AdxPeriod { get; init; }
    public int AdxThreshold { get; init; }
}