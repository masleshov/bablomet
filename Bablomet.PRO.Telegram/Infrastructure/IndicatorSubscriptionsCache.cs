using System.Collections.Concurrent;
using Bablomet.Common.Infrastructure;
using Bablomet.Common.Web;
using Bablomet.PRO.Telegram.Repository;
using Telegram.Bot.Types;

namespace Bablomet.PRO.Telegram.Infrastructure;

public static class IndicatorSubscriptionsCache
{
    private static volatile bool _initialized;
    private static SubscriptionRepository _repository;

    // subscription -> chat id
    private static ConcurrentDictionary<IndicatorSubscription, ConcurrentDictionary<long, byte>> _subscriptions;

    public static void Initialize(UnitOfWork uow)
    {
        if (_initialized) throw new InvalidOperationException("IndicatorSubscriptionsCache already initialized!");
        if (uow == null) throw new ArgumentNullException(nameof(uow));

        _repository = uow.GetRepository<SubscriptionRepository>();
        _subscriptions = new ConcurrentDictionary<IndicatorSubscription, ConcurrentDictionary<long, byte>>(_repository.GetAllSubscriptions()
            .GroupBy(s => new IndicatorSubscription(s.IndicatorType, s.Symbol, s.TimeFrame, s.Parameters))
            .ToDictionary(
                key => key.Key, 
                val => new ConcurrentDictionary<long, byte>(val.Select(s => s.ChatId).ToDictionary(sk => sk, _ => (byte)0)))
            );
    }

    public static void Subscribe(IndicatorSubscription subscription, long chatId)
    {
        if (!_subscriptions.TryGetValue(subscription, out var chats))
        {
            _subscriptions[subscription] = chats = new ConcurrentDictionary<long, byte>();
        }

        chats.TryAdd(chatId, 0);

        _repository.AddSubscriptionIfNotExists(new Domain.Subscription
        {
            IndicatorType = subscription.IndicatorType,
            Symbol = subscription.Symbol,
            TimeFrame = subscription.TimeFrame,
            Parameters = subscription.Parameters,
            ChatId = chatId
        });
    }

    public static void Unsubscribe(IndicatorSubscription subscription, long chatId)
    {
        if (!_subscriptions.TryGetValue(subscription, out var chats))
        {
            return;
        }

        chats.TryRemove(chatId, out _);
        if (chats.Count == 0)
        {
            _subscriptions.TryRemove(subscription, out _);
        }

        _repository.RemoveSubscription(new Domain.Subscription
        {
            IndicatorType = subscription.IndicatorType,
            Symbol = subscription.Symbol,
            TimeFrame = subscription.TimeFrame,
            Parameters = subscription.Parameters
        });
    }

    public static long[] GetSubscribedChats(IndicatorSubscription subscription)
    {
        if (!_subscriptions.TryGetValue(subscription, out var chats))
        {
            return Array.Empty<long>();
        }

        return chats.Keys.ToArray();
    }
}