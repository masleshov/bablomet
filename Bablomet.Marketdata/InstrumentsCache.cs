using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bablomet.Marketdata.External;
using Bablomet.Marketdata.WebSocket;

namespace Bablomet.Marketdata;

public static class InstrumentsCache
{
    private static readonly ConcurrentDictionary<Guid, string> _subscriptionToTicker = new();
    
    public static ReadOnlyDictionary<string, InstrumentResponseDto> Instruments { get; private set; }

    public static async Task Init(IAlorClient alorClient)
    {
        if (alorClient == null)
        {
            throw new ArgumentNullException(nameof(alorClient));
        }

        var result = await alorClient.GetInstruments("MOEX", "FOND", "TQBR", 1000, 0);
        Instruments = result.ToDictionary(key => key.Symbol, val => val).AsReadOnly();
    }

    public static void SetSubscription(Guid subscriptionId, string ticker)
    {
        if (subscriptionId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(subscriptionId));
        }

        if (string.IsNullOrEmpty(ticker))
        {
            throw new ArgumentNullException(nameof(ticker));
        }

        if (_subscriptionToTicker.TryGetValue(subscriptionId, out _))
        {
            throw new InvalidOperationException($"Ticker {ticker} is already subscribed through subscription {subscriptionId}");
        }

        _subscriptionToTicker[subscriptionId] = ticker;
    }

    public static bool TryUpdateInstrument(InstrumentUpdateDto update)
    {
        if (!_subscriptionToTicker.TryGetValue(update.Guid, out var ticker))
        {
            return false;
        }

        if (!Instruments.TryGetValue(ticker, out var instrument))
        {
            throw new NullReferenceException($"Ticker {ticker} does not exist in the cache!");
        }

        instrument.PriceMax = update.Data.PriceMax;
        instrument.PriceMin = update.Data.PriceMin;
        instrument.MarginBuy = update.Data.MarginBuy;
        instrument.MarginSell = update.Data.MarginSell;
        instrument.TradingStatus = update.Data.TradingStatus;
        instrument.TradingStatusInfo = update.Data.TradingStatusInfo;
        instrument.TheorPrice = update.Data.TheorPrice;
        instrument.TheorPriceLimit = update.Data.TheorPriceLimit;
        instrument.Volatility = update.Data.Volatility;

        return true;
    }
}