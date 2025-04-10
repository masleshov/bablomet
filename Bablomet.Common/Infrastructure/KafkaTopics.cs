using System;

namespace Bablomet.Common.Infrastructure;

public static class KafkaTopics
{
    private static readonly string BarsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_BARS_TOPIC);

    public static string GetBarTopic(string symbol, string timeFrame)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentNullException(nameof(symbol));
        if (string.IsNullOrWhiteSpace(timeFrame)) throw new ArgumentNullException(nameof(timeFrame));

        return $"{BarsTopic}-{symbol.ToUpper()}-{timeFrame}";
    }
}