namespace Bablomet.API.Indicators;

public record ExponentialMovingAverage
{
    public string Symbol { get; init; }
    public string TimeFrame { get; init; }
    public long Time { get; init; }
    public decimal Value { get; init; }
}