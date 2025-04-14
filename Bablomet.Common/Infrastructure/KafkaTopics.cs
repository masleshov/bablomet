namespace Bablomet.Common.Infrastructure;

public static class KafkaTopics
{
    public static readonly string BarsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_BARS_TOPIC);
    public static readonly string IndicatorsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_INDICATORS_TOPIC);

    public static readonly string BarsAiTrainingTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_BARS_AI_TRAINING_TOPIC);
    public static readonly string IndicatorsAiTrainingTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_INDICATORS_AI_TRAINING_TOPIC);
}