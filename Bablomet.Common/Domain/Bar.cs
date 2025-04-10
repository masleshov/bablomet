namespace Bablomet.Common.Domain;

public class Bar
{
    public long BarId { get; set; }
    public string Symbol { get; set; }
    public string TimeFrame { get; set; }
    public long Time { get; set; }
    public decimal Close { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
}