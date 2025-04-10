using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bablomet.Common.Infrastructure;
using Bablomet.Marketdata.Infrastructure;
using Bablomet.Marketdata.Mapping;
using Bablomet.Marketdata.Repository;
using Bablomet.Marketdata.WebSocket;

namespace Bablomet.Marketdata.Subscriber;

public sealed class InstrumentSubscriber
{
    private readonly InstrumentRepository _repository;
    private readonly WebSocketSharp.WebSocket _socket;
    private readonly ConcurrentDictionary<Guid, SubscribeInstrumentsDto> _subscriptions;

    public InstrumentSubscriber(WebSocketSharp.WebSocket socket, UnitOfWork uow)
    {
        _socket = socket;
        _repository = uow.GetRepository<InstrumentRepository>();
        _subscriptions = new ConcurrentDictionary<Guid, SubscribeInstrumentsDto>();
    }

    public async Task Subscribe()
    {
        foreach (var ticker in InstrumentsCache.Instruments.Keys)
        {
            var command = new SubscribeInstrumentsDto
            {
                Token = AlorJwtHolder.Jwt,
                Code = ticker,
                Exchange = "MOEX",
            };

            _subscriptions.TryAdd(command.Guid, command);
            InstrumentsCache.SetSubscription(command.Guid, command.Code);
            _socket.Send(JsonSerializer.Serialize(command));

            await Task.Delay(100);
        }
    }

    public async Task OnMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        var instrumentUpdate = JsonSerializer.Deserialize<InstrumentUpdateDto>(message);
        if (!_subscriptions.TryGetValue(instrumentUpdate.Guid, out var subscription))
        {
            return;
        }

        InstrumentsCache.TryUpdateInstrument(instrumentUpdate);
        var instrument = InstrumentMapper.ToDomain(InstrumentsCache.Instruments.GetValueOrDefault(subscription.Code));
        
        _repository.UpdateInstrument(instrument);
    }
}