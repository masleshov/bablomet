namespace Bablomet.API.Indicators;

public class MovingAverageConvergenceDivergence
{
    public string Symbol { get; init; }
    public string TimeFrame { get; init; }
    public long Time { get; init; }
    public decimal MacdLine { get; init; }
    public decimal SignalLine { get; init; }
    public decimal Histogram { get; init; }
}