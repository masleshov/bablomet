using System;
using System.Text.Json;
using System.Threading.Tasks;
using Bablomet.Common.Infrastructure;

namespace Bablomet.API.Infrastructure;

public class IndicatorPublisher
{
    private readonly KafkaConnector _kafkaConnector;
    private readonly string _indicatorsTopic;

    public IndicatorPublisher()
    {
        _indicatorsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_INDICATORS_TOPIC);
        _kafkaConnector = new KafkaConnector();
    }

    public async Task PublishIndicatorAsync<TIndicator>(TIndicator indicator) where TIndicator : class
    {
        var message = JsonSerializer.Serialize(indicator);
        await _kafkaConnector.Send(_indicatorsTopic, message);
    }
}