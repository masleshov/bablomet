using System;
using System.Text.Json;
using System.Threading.Tasks;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;

namespace Bablomet.API.Infrastructure;

public class IndicatorPublisher
{
    private readonly KafkaConnector<KafkaBarKey, string> _kafkaConnector;
    private readonly string _indicatorsTopic;

    public IndicatorPublisher()
    {
        _indicatorsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_INDICATORS_TOPIC);
        _kafkaConnector = new KafkaConnector<KafkaBarKey, string>();
    }

    public async Task PublishIndicatorAsync<TIndicator>(TIndicator indicator) 
        where TIndicator : class, IIndicatorSignal
    {
        var message = JsonSerializer.Serialize(indicator);
        await _kafkaConnector.Send(_indicatorsTopic, new KafkaBarKey(indicator.Symbol, indicator.TimeFrame), message);
    }
}