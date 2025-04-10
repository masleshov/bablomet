using Bablomet.Common.Infrastructure;

namespace Bablomet.Common.Domain;

public sealed class IndicatorSignal
{
    public IndicatorType IndicatorType { get; set; }
    public int[] Parameters { get; set; }
    public string Symbol { get; set; }
    public string TimeFrame { get; set; }
    public long Time { get; set; }
    public CrossoverSignal Signal { get; set; }
}