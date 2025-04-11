using Bablomet.AI.ML.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .Build();

var cancellationTokenSource = new CancellationTokenSource();
var kafkaConsumer = new KafkaConsumer();
await kafkaConsumer.Start(cancellationTokenSource.Token);

await host.RunAsync(cancellationTokenSource.Token);