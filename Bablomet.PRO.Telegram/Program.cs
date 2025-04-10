// Файл: Program.cs (часть регистрации сервисов)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Bablomet.PRO.Telegram.Handlers;
using Bablomet.PRO.Telegram.Services;
using Bablomet.PRO.Telegram.UI;
using Bablomet.Common.Infrastructure;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Refit;
using System.Text.Json;
using Bablomet.Common.Domain;
using Bablomet.PRO.Telegram.Infrastructure;
using Npgsql;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<UnitOfWork>();

        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(EnvironmentGetter.GetVariable(EnvironmentVariables.BABLOMET_PRO_TELEGRAM_TOKEN)));

        services.AddRefitClient<IIndicatorsApi>(new Uri(EnvironmentGetter.GetVariable(EnvironmentVariables.BABLOMET_API_URI)));

        services.AddSingleton<TickerService>();
        services.AddSingleton<TimeframeService>();
        services.AddSingleton<MenuGenerator>();
        services.AddSingleton<SignalSettingsService>();
        services.AddSingleton<IndicatorSignalService>();
        services.AddSingleton<UpdateHandler>();
    })
    .Build();

IndicatorSubscriptionsCache.Initialize(host.Services.GetRequiredService<UnitOfWork>());

var botClient = host.Services.GetRequiredService<ITelegramBotClient>();
var updateHandler = host.Services.GetRequiredService<UpdateHandler>();

var kafkaConnector = new KafkaConnector();
var indicatorsTopic = EnvironmentGetter.GetVariable(EnvironmentVariables.KAFKA_INDICATORS_TOPIC);
var indicatorSignalService = host.Services.GetRequiredService<IndicatorSignalService>();
kafkaConnector.StartListen(new Dictionary<string, Func<string, Task>>
{
    {
        indicatorsTopic, async message =>
        {
            var signal = JsonSerializer.Deserialize<IndicatorSignal>(message);
            await indicatorSignalService.HandleAsync(signal);
        }
    }
}, CancellationToken.None);

botClient.StartReceiving(
    updateHandler.HandleUpdateAsync,
    updateHandler.HandleErrorAsync,
    new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
    CancellationToken.None
);

Console.WriteLine("Bablomet PRO Telegram Bot запущен.");
await Task.Delay(-1);
