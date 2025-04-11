namespace Bablomet.Common.Domain;

public interface IIndicatorSignal
{
    string Symbol { get; set; }
    string TimeFrame { get; set; }
}