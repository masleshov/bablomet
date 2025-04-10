using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
using Bablomet.Marketdata.Repository;
using Bablomet.Marketdata.WebSocket;
using Microsoft.Extensions.Logging;

namespace Bablomet.Marketdata.Subscriber;

public sealed class BarSubscriber
{
    private static readonly bool SaveBarsToDatabase = EnvironmentGetter.GetBoolVariable(EnvironmentVariables.BABLOMET_MARKETDATA_SAVE_BARS_TO_DB);
    
    private readonly WebSocketSharp.WebSocket _socket;
    private readonly BarRepository _repository;
    private readonly CancellationToken _token;
    private readonly ConcurrentDictionary<Guid, SubscribeBarsDto> _subscriptions;
    private readonly ConcurrentDictionary<string, BufferBlock<Bar>> _queues;

    private readonly BufferBlock<string> _routerQueue;
    private readonly ILogger _logger;

    public BarSubscriber(WebSocketSharp.WebSocket socket, UnitOfWork uow, ILogger logger, CancellationToken token)
    {
        _socket = socket;
        _repository = uow.GetRepository<BarRepository>();
        _token = token;
        _subscriptions = new ConcurrentDictionary<Guid, SubscribeBarsDto>();

        _routerQueue = new BufferBlock<string>();
        _queues = new ConcurrentDictionary<string, BufferBlock<Bar>>();

        _logger = logger;

        StartProcessingRouterQueue();
    }

    public async Task Subscribe(string[] timeFrames)
    {
        if (timeFrames == null || timeFrames.Length == 0) throw new ArgumentNullException(nameof(timeFrames));
        
        foreach (var timeFrame in timeFrames)
        {
            foreach (var ticker in InstrumentsCache.Instruments.Keys)
            {
                var command = new SubscribeBarsDto
                {
                    Code = ticker,
                    Exchange = "MOEX",
                    TimeFrame = timeFrame,
                    From = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds()
                };
                _subscriptions.TryAdd(command.Guid, command);
                _socket.Send(JsonSerializer.Serialize(command));
            }

        }
    }

    public async Task OnMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

        await _routerQueue.SendAsync(message);
    }

    private void StartProcessingRouterQueue()
    {
        Task.Run(async () =>
        {
            while (!_token.IsCancellationRequested)
            {
                var message = await _routerQueue.ReceiveAsync();
                if (string.IsNullOrWhiteSpace(message)) continue;

                var barDto = JsonSerializer.Deserialize<BarDto>(message);
                if (barDto == null) continue;

                if (!_subscriptions.TryGetValue(barDto.Guid, out var subscription))
                {
                    continue;
                }

                if (!_queues.TryGetValue(subscription.TimeFrame, out var queue))
                {
                    _queues[subscription.TimeFrame] = queue = new BufferBlock<Bar>();
                    StartProcessingQueue(queue);
                }

                await queue.SendAsync(new Bar
                {
                    Symbol = subscription.Code,
                    TimeFrame = subscription.TimeFrame,
                    Time = barDto.Data.Time,
                    Close = barDto.Data.Close,
                    Open = barDto.Data.Close,
                    High = barDto.Data.High,
                    Low = barDto.Data.Low,
                    Volume = barDto.Data.Volume
                });
            }
        });
    }

    private void StartProcessingQueue(BufferBlock<Bar> queue)
    {
        if (queue == null) throw new ArgumentNullException(nameof(queue));

        Task.Run(async () => 
        {
            var kafkaConnector = new KafkaConnector();
            while (!_token.IsCancellationRequested)
            {
                var bar = await queue.ReceiveAsync();
                if (bar == null) continue;

                if (SaveBarsToDatabase)
                {
                    bar.BarId = _repository.AddBarIfNotExists(bar);
                }

                _logger.LogInformation($"Received bar {JsonSerializer.Serialize(bar)}");
                await kafkaConnector.Send(KafkaTopics.GetBarTopic(bar.Symbol, bar.TimeFrame), JsonSerializer.Serialize(bar));
            }
        });
    }
}