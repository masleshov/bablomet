using System;
using Confluent.Kafka;
using MessagePack;

namespace Bablomet.Common.Infrastructure;

[MessagePackObject]
public class KafkaBarKey
{
    [Key(0)]
    public readonly string Ticker;
    [Key(1)]
    public readonly string TimeFrame;

    public KafkaBarKey(string ticker, string timeframe)
    {
        if (string.IsNullOrWhiteSpace(ticker)) throw new ArgumentNullException(nameof(ticker));
        if (string.IsNullOrWhiteSpace(timeframe)) throw new ArgumentNullException(nameof(timeframe));

        Ticker = ticker;
        TimeFrame = timeframe;
    }
}