using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
using Confluent.Kafka;

namespace Bablomet.AI.ML.Kafka;

public class KafkaConsumer
{
    private readonly KafkaConnector<KafkaBarKey, string> _kafkaConnector;

    public KafkaConsumer()
    {
        _kafkaConnector = new KafkaConnector<KafkaBarKey, string>();
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        _kafkaConnector.StartListen(new Dictionary<string, Func<Message<KafkaBarKey, string>, Task>>
        {
            { KafkaTopics.BarsTopic, async message =>
                {
                    try
                    {
                        var bar = JsonSerializer.Deserialize<Bar>(message.Value);
                        if (bar != null)
                        {
                            await HandleBarAsync(bar);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while handling bar message: {ex.Message}");
                        Console.WriteLine(ex.StackTrace ?? string.Empty);
                    }
                }
            },
            { KafkaTopics.IndicatorsTopic, async message =>
                {
                    try
                    {
                        var signal = JsonSerializer.Deserialize<IndicatorSignal>(message.Value);
                        if (signal != null)
                        {
                            await HandleIndicatorAsync(signal);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while handling indicator signal: {ex.Message}");
                        Console.WriteLine(ex.StackTrace ?? string.Empty);
                    }
                }
            }
        }, cancellationToken);

        Console.WriteLine($"Kafka consumer started and listening to topics: {KafkaTopics.BarsTopic} {KafkaTopics.IndicatorsTopic}");
    }

    private Task HandleBarAsync(Bar bar)
    {
        Console.WriteLine($"Received bar: {bar.Symbol} {bar.TimeFrame} {bar.Close}");
        // TODO: Forward to inference processors
        return Task.CompletedTask;
    }

    private Task HandleIndicatorAsync(IndicatorSignal signal)
    {
        Console.WriteLine($"Received signal: {signal.IndicatorType} {signal.Symbol} {signal.Signal}");
        // TODO: Forward to inference processors
        return Task.CompletedTask;
    }
}