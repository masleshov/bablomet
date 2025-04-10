using System;
using System.Collections.Concurrent;
using System.Linq;
using Bablomet.Common.Web;

namespace Bablomet.API.Infrastructure;

public static class IndicatorSubscriptionsCache
{
    private static readonly ConcurrentDictionary<BarKey, ConcurrentDictionary<IndicatorSubscription, byte>> _barKeys = new();
    private static readonly ConcurrentDictionary<IndicatorSubscription, byte> _subscriptions = new();

    public static void Subscribe(IndicatorSubscription subscription)
    {
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));

        _subscriptions.TryAdd(subscription, 0);

        var barKey = new BarKey(subscription.Symbol, subscription.TimeFrame);
        if (!_barKeys.TryGetValue(barKey, out var subscriptions))
        {
            _barKeys[barKey] = subscriptions = new ConcurrentDictionary<IndicatorSubscription, byte>();
        }

        subscriptions.TryAdd(subscription, 0);

        Console.WriteLine($"IndicatorSubscriptionsCache.Subscribe | _subscriptions.Count = {_subscriptions.Count}, _barKeys.Count = {_barKeys.Count}");
    }

    public static void Unsubscribe(IndicatorSubscription subscription)
    {
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));

        _subscriptions.TryRemove(subscription, out _);

        var barKey = new BarKey(subscription.Symbol, subscription.TimeFrame);
        if (!_barKeys.TryGetValue(barKey, out var subscriptions))
        {
            return;
        }

        subscriptions.TryRemove(subscription, out _);

        Console.WriteLine($"IndicatorSubscriptionsCache.Unsubscribe | _subscriptions.Count = {_subscriptions.Count}, _barKeys.Count = {_barKeys.Count}");
    }

    public static IndicatorSubscription[] GetSubscriptionsForBar(BarKey barKey)
    {
        if (!_barKeys.TryGetValue(barKey, out var subscriptions))
        {
            return Array.Empty<IndicatorSubscription>();
        }

        return subscriptions.Keys.ToArray();
    }
}