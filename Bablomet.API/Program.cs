using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bablomet.API;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
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

var kafkaConnector = new KafkaConnector();
var timeFrames = new [] { TimeFrames.Minute, TimeFrames.Minutes5, TimeFrames.Minutes15, TimeFrames.Minutes60, TimeFrames.Days };

var topicTasks = InstrumentsCache.GetAllInstruments()
    .SelectMany(i => timeFrames.Select(tf => KafkaTopics.GetBarTopic(i.Symbol, tf)))
    .Select(topic => new
    {
        Topic = topic,
        CalculationService = app.Services.GetRequiredService<IndicatorCalculationService>()
    })
    .ToDictionary(
        key => key.Topic,
        val => new Func<string, Task>(async message => 
        {
            var bar = JsonSerializer.Deserialize<Bar>(message)!;
            await val.CalculationService.ProcessBarAsync(bar);
        })
    );

kafkaConnector.StartListen(topicTasks, CancellationToken.None);

await app.RunAsync();