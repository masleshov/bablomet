using Bablomet.Common.Infrastructure;

namespace Bablomet.PRO.Telegram.Domain;

public sealed class Subscription
{
    public long SubscriptionId { get; set; }
    public IndicatorType IndicatorType { get; set; } 
    public string Symbol { get; set; }
    public string TimeFrame { get; set; }
    public int[] Parameters { get; set; }
    public long ChatId { get; set; }
}