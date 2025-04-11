namespace Bablomet.Common.Infrastructure;

public static class KafkaTopics
{
    public static readonly string BarsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_BARS_TOPIC);
    public static readonly string IndicatorsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_INDICATORS_TOPIC);
}