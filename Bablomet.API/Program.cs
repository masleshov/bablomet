using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Bablomet.API;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;

ThreadPool.SetMaxThreads(32000, 32000);
ThreadPool.SetMinThreads(32000, 32000);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Any, 5001);
});


// Add services to the container.
builder.Services.AddTransient<UnitOfWork>();
builder.Services.AddSingleton<IndicatorPublisher>();
builder.Services.AddSingleton<IndicatorCalculationService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await InstrumentsCache.Init(app.Services.GetRequiredService<UnitOfWork>());

var kafkaConnector = new KafkaConnector<KafkaBarKey, string>();
var timeFrames = new [] { TimeFrames.Minute, TimeFrames.Minutes5, TimeFrames.Minutes15, TimeFrames.Minutes60, TimeFrames.Days };

var tokenSource = new CancellationTokenSource();
var queues = new Dictionary<KafkaBarKey, BufferBlock<string>>();
var aiTrainingQueues = new Dictionary<KafkaBarKey, BufferBlock<string>>();
kafkaConnector.StartListen(new Dictionary<string, Func<Message<KafkaBarKey, string>, Task>>
{
    { KafkaTopics.BarsTopic, async message => 
    {
        if (!queues.TryGetValue(message.Key, out var queue))
        {
            queues[message.Key] = queue = new BufferBlock<string>();
            StartCalculatingIndicators(queue, false);
        }

        await queue.SendAsync(message.Value);
    }},
    { KafkaTopics.BarsAiTrainingTopic, async message => 
    {
        if (!aiTrainingQueues.TryGetValue(message.Key, out var queue))
        {
            aiTrainingQueues[message.Key] = queue = new BufferBlock<string>();
            StartCalculatingIndicators(queue, true);
        }

        await queue.SendAsync(message.Value);
    }}
}, tokenSource.Token);

await app.RunAsync();

tokenSource.Cancel();

void StartCalculatingIndicators(BufferBlock<string> queue, bool aiTraining)
{
    if (queue == null) throw new ArgumentNullException(nameof(queue));

    _ = Task.Run(async () => 
    {
        var service = app.Services.GetRequiredService<IndicatorCalculationService>();
        while (!tokenSource.IsCancellationRequested)
        {
            var str = await queue.ReceiveAsync();
            if (string.IsNullOrWhiteSpace(str)) continue;

            var bar = JsonSerializer.Deserialize<Bar>(str)!;
            await service.ProcessBarAsync(bar, aiTraining);
        }
    });
}