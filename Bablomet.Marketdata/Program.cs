using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
using Bablomet.Marketdata;
using Bablomet.Marketdata.External;
using Bablomet.Marketdata.Infrastructure;
using Bablomet.Marketdata.Mapping;
using Bablomet.Marketdata.Repository;
using Bablomet.Marketdata.Subscriber;
using Bablomet.Marketdata.WebSocket;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebSocketSharp;
using InstrumentsCache = Bablomet.Marketdata.InstrumentsCache;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddTransient<UnitOfWork>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<HttpAuthenticatingHandler>();

builder.Services.AddRefitClient<IAlorOauthClient>(new Uri("https://oauth.alor.ru"));
builder.Services.AddRefitClient<IAlorClient>(new Uri("https://api.alor.ru"))
    .AddHttpMessageHandler<HttpAuthenticatingHandler>();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/diagnostic/health");

#if !DEBUG
    EnvironmentSetter.SetFile("../.env");
#endif

var alorClient = app.Services.GetRequiredService<IAlorClient>();
var alorOauthClient = app.Services.GetRequiredService<IAlorOauthClient>();
await AlorJwtHolder.Init(alorOauthClient);
Console.WriteLine("Jwt: " + AlorJwtHolder.Jwt);

await KafkaConnector<Null, Null>.CreateTopics(KafkaTopics.BarsTopic, KafkaTopics.IndicatorsTopic);

var uow = app.Services.GetRequiredService<UnitOfWork>();
await InstrumentsCache.Init(alorClient);

var backtestFromStr = EnvironmentGetter.GetVariable(EnvironmentVariables.BACKTEST_FROM);
var backtestFrom = string.IsNullOrWhiteSpace(backtestFromStr)
    ? (DateOnly?)null
    : DateOnly.Parse(backtestFromStr);

var instrumentRepository = uow.GetRepository<InstrumentRepository>();
foreach (var instrument in InstrumentsCache.Instruments.Values)
{
    await instrumentRepository.AddInstrumentIfNotExists(InstrumentMapper.ToDomain(instrument));
}

var logger = app.Logger;

var timeFrames = new [] { TimeFrames.Minute, TimeFrames.Minutes5, TimeFrames.Minutes15, TimeFrames.Minutes60, TimeFrames.Days };

var webSockets = new List<WebSocket>();
InstrumentSubscriber instrumentSubscriber = null;
var ws = WebSocketHelper.Subscribe(
    url: "wss://api.alor.ru/ws",
    onOpen: async socket => 
    {
        instrumentSubscriber = new InstrumentSubscriber(socket, app.Services.GetRequiredService<UnitOfWork>());
        await instrumentSubscriber.Subscribe();
        
        logger.LogInformation($"WebSocket connection for instruments opened!");
    },
    onMessage: async e => 
    {
        logger.LogInformation($"Received an update of an instrument: {e.Data}");

        await instrumentSubscriber.OnMessage(e.Data);
    },
    onClose: async e => 
    {
        logger.LogInformation($"WebSocket connection for instruments closed! Reason: {e.Reason}");
    },
    onError: async e => 
    {
        logger.LogError($"WebSocket error: {e.Message}");
    }
);
webSockets.Add(ws);

var tokenSource = new CancellationTokenSource();
ws = new WebSocketSharp.WebSocket("wss://api.alor.ru/ws");
var barSubscriber = new BarSubscriber(
    socket: ws, 
    uow: app.Services.GetRequiredService<UnitOfWork>(), 
    logger: app.Logger,
    token: tokenSource.Token
);
WebSocketHelper.Subscribe(
    ws: ws,
    onOpen: async socket => 
    {
        if (!backtestFrom.HasValue)
        {
            await barSubscriber.Subscribe(timeFrames);
        }

        logger.LogInformation($"WebSocket connection opened!");
        
    },
    onMessage: async e => 
    {
        await barSubscriber.OnMessage(e.Data);
    },
    onClose: async e => 
    {
        logger.LogInformation($"WebSocket connection for bars closed! Reason: {e.Reason}");
        tokenSource.Cancel();
    },
    onError: async e => 
    {
        logger.LogError($"WebSocket error: {e.Message}");
        tokenSource.Cancel();
    }
);
webSockets.Add(ws);

app.MapGet("/api/history", async (string symbol, string exchange, string tf, long from, long to, IAlorClient alorClient) => 
{
    return (await alorClient.GetBarsHistory(symbol, exchange, tf, from, to))
        .History
        .Select(dto => new Bar
        {
            Symbol = symbol,
            TimeFrame = tf,
            Time = dto.Time,
            Close = dto.Close,
            Open = dto.Close,
            High = dto.High,
            Low = dto.Low,
            Volume = dto.Volume
        })
        .ToArray();
});

var host = app.RunAsync("http://0.0.0.0:5000");

if (backtestFrom.HasValue)
{
    Console.WriteLine($"Started producing historical data since {backtestFrom.Value}");
    timeFrames = new[] { TimeFrames.Minute, TimeFrames.Minutes5, TimeFrames.Minutes15, TimeFrames.Minutes60, TimeFrames.Days };
    var kafkaConnector = new KafkaConnector<KafkaBarKey, string>();

    var current = backtestFrom.Value;

    while (current != DateOnly.FromDateTime(DateTime.UtcNow))
    {
        var tasks = new List<Task>();
        foreach (var ticker in InstrumentsCache.Instruments.Keys)
        {
            foreach (var tf in timeFrames)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var from = new DateTime(current.Year, current.Month, current.Day);
                    var to = from.AddDays(1).AddMilliseconds(-1);
                    var fromUnix = ((DateTimeOffset)from).ToUnixTimeSeconds();
                    var toUnix = ((DateTimeOffset)to).ToUnixTimeSeconds();
                    
                    try
                    {
                        var history = (await alorClient.GetBarsHistory(ticker, "MOEX", tf, fromUnix, toUnix))
                            .History
                            .OrderBy(bar => bar.Time)
                            .ToArray();
                        
                        Console.WriteLine($"Got {history.Length} bars for {ticker} {current} - {tf}");

                        foreach (var bar in history)
                        {
                            await kafkaConnector.Send(KafkaTopics.BarsTopic, new KafkaBarKey(ticker, tf), JsonSerializer.Serialize(new Bar
                            {
                                Symbol = ticker,
                                TimeFrame = tf,
                                Time = bar.Time,
                                Close = bar.Close,
                                Open = bar.Open,
                                High = bar.High,
                                Low = bar.Low,
                                Volume = bar.Volume
                            }));
                            await Task.Delay(100);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace ?? string.Empty);
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);
        current = current.AddDays(1);
    }
}

await host;

webSockets.ForEach(ws => ws.Close());