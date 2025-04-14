using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Bablomet.AI.ML.Constants;
using Bablomet.AI.ML.Training.Loaders;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
using Confluent.Kafka;

namespace Bablomet.AI.ML.Training.Pipeline;

public sealed class DefaultTrainingPipeline
{
    private readonly HistoricalBarLoader _barLoader;

    public DefaultTrainingPipeline(HistoricalBarLoader barLoader)
    {
        _barLoader = barLoader;
    }

    public async Task StartModelTraining(CancellationToken token)
    {
        var kafkaConnector = new KafkaConnector<KafkaBarKey, string>();
        kafkaConnector.StartListen(new Dictionary<string, Func<Message<KafkaBarKey, string>, Task>>
        {
            { KafkaTopics.IndicatorsAiTrainingTopic, async message => 
            {
                // push forward the indicator to model
            }}
        }, token);

        var timeFrames = new [] { TimeFrames.Minute, TimeFrames.Minutes5, TimeFrames.Minutes15, TimeFrames.Minutes60, TimeFrames.Days };
        var instruments = InstrumentsCache.GetAllInstruments();

        foreach (var timeFrame in timeFrames)
        {
            foreach (var instrument in instruments)
            {
                var fileFolder = $"{HistoricalDataConstants.BarFilesFolder}/{instrument.Symbol}/{timeFrame}";
                var lastTimestampFile = $"{fileFolder}/{HistoricalDataConstants.BarLastProcessedTimestampFileName}";

                var lastTimestamp = File.Exists(lastTimestampFile)
                    ? Convert.ToInt64(await File.ReadAllTextAsync(lastTimestampFile, token))
                    : DateTimeOffset.UtcNow.AddMonths(-12).ToUnixTimeSeconds();

                var queue = new BufferBlock<Bar[]>();
                _ = Task.Run(async () => 
                {
                    while (!token.IsCancellationRequested)
                    {
                        var chunk = await queue.ReceiveAsync();
                        foreach (var bar in chunk)
                        {
                            await kafkaConnector.Send(
                                topic: KafkaTopics.BarsAiTrainingTopic, 
                                key: new KafkaBarKey(bar.Symbol, bar.TimeFrame),
                                message: JsonSerializer.Serialize(bar)
                            );
                        }
                    }
                }, token);

                await _barLoader.LoadAndCacheBarsAsync(
                    symbol: instrument.Symbol, 
                    exchange: instrument.Exchange, 
                    tf: timeFrame, 
                    path: fileFolder, 
                    from: DateTimeOffset.FromUnixTimeSeconds(lastTimestamp),
                    barQueue: queue
                );
            }
        }
    }
}