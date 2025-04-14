using System;
using System.Text.Json;
using System.Threading.Tasks;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;

namespace Bablomet.API.Infrastructure;

public class IndicatorPublisher
{
    private readonly KafkaConnector<KafkaBarKey, string> _kafkaConnector;

    public IndicatorPublisher()
    {
        _kafkaConnector = new KafkaConnector<KafkaBarKey, string>();
    }

    public async Task PublishIndicatorAsync<TIndicator>(TIndicator indicator, bool aiTraining) 
        where TIndicator : class, IIndicatorSignal
    {
        var message = JsonSerializer.Serialize(indicator);

        var topic = aiTraining ? KafkaTopics.IndicatorsAiTrainingTopic : KafkaTopics.IndicatorsTopic;
        await _kafkaConnector.Send(topic, new KafkaBarKey(indicator.Symbol, indicator.TimeFrame), message);
    }
}