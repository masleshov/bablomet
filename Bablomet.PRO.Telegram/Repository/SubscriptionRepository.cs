using System.Diagnostics.SymbolStore;
using Bablomet.Common.Repository;
using Bablomet.PRO.Telegram.Domain;
using Dapper;
using Npgsql;

namespace Bablomet.PRO.Telegram.Repository;

public sealed class SubscriptionRepository : BaseRepository
{
    public SubscriptionRepository(NpgsqlConnection connection) : base(connection)
    {
    }

    public Subscription[] GetAllSubscriptions()
    {
        var query = "select subscription_id as SubscriptionId " +
                    "     , indicator_type as IndicatorType " +
                    "     , symbol as Symbol " +
                    "     , time_frame as TimeFrame " +
                    "     , parameters as Parameters " +
                    "     , chat_id as ChatId " +
                    "from telegram_bot_subscriptions; ";

        return Connection.Query<Subscription>(query).ToArray();
    } 
    
    public int AddSubscriptionIfNotExists(Subscription subscription)
    {
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));
        if (subscription.SubscriptionId > 0) throw new ArgumentNullException(nameof(subscription));

        var query =
            "insert into telegram_bot_subscriptions (indicator_type, symbol, time_frame, parameters, chat_id) " +
            "select @IndicatorType::indicator_type_et, @Symbol, @TimeFrame, @Parameters, @ChatId " +
            "returning subscription_id; ";

        return Connection.QuerySingleOrDefault<int>(query, new
        {
            IndicatorType = subscription.IndicatorType.ToString(),
            subscription.Symbol,
            subscription.TimeFrame,
            subscription.Parameters,
            subscription.ChatId
        });
    }

    public void RemoveSubscription(Subscription subscription)
    {
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));
        if (subscription.SubscriptionId == 0) throw new ArgumentNullException(nameof(subscription));

        var query = "delete from telegram_bot_subscriptions " +
                    "where indicator_type = @IndicatorType::indicator_type_et " +
                    "  and symbol = @Symbol " +
                    "  and time_frame = @TimeFrame " +
                    "  and parameters = @Parameters " +
                    "  and chat_id = @ChatId; ";

        Connection.Execute(query, new
        {
            IndicatorType = subscription.IndicatorType.ToString(),
            subscription.Symbol,
            subscription.TimeFrame,
            subscription.Parameters,
            subscription.ChatId
        });
    }
}