using Bablomet.Common.Web;
using Refit;

public interface IIndicatorsApi
{
    [Post("/indicators/subscribe")]
    Task SubscribeAsync([Body] IndicatorSubscription subscription);

    [Post("/indicators/unsubscribe")]
    Task UnsubscribeAsync([Body] IndicatorSubscription subscription);
}