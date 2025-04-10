using System.Text;
using Telegram.Bot;
using Bablomet.Common.Domain;
using Bablomet.PRO.Telegram.Infrastructure;
using Bablomet.Common.Web;
using Telegram.Bot.Types.Enums;

namespace Bablomet.PRO.Telegram.Services;

public class IndicatorSignalService
{
    private readonly ITelegramBotClient _botClient;

    public IndicatorSignalService(ITelegramBotClient botClient)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
    }

    public async Task HandleAsync(IndicatorSignal signal)
    {
        var subscribers = IndicatorSubscriptionsCache.GetSubscribedChats(new IndicatorSubscription(
            indicatorType: signal.IndicatorType,
            symbol: signal.Symbol,
            timeFrame: signal.TimeFrame,
            parameters: signal.Parameters
        ));
        if (subscribers == null || subscribers.Length == 0)
        {
            return;
        }

        var message = BuildMessage(signal);
        foreach (var chatId in subscribers)
        {
            try
            {
                await _botClient.SendMessage(chatId, message, parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send message to {chatId}: {ex.Message}");
            }
        }
    }

    private static string BuildMessage(IndicatorSignal signal)
    {
        var builder = new StringBuilder();

        var (icon, directionText) = signal.Signal switch
        {
            CrossoverSignal.Bullish => ("📈", "Сигнал на повышение"),
            CrossoverSignal.Bearish => ("📉", "Сигнал на понижение"),
            _ => ("ℹ️", "Информационный сигнал")
        };

        builder.AppendLine($"📢 {icon} *{directionText}*");
        builder.AppendLine($"Индикатор: *{signal.IndicatorType}*");
        builder.AppendLine($"Инструмент: *{signal.Symbol}*");

        var tfText = TimeframeService.ToUserFriendly(signal.TimeFrame);
        builder.AppendLine($"Таймфрейм: *{tfText}*");

        builder.AppendLine($"Параметры: *{string.Join(", ", signal.Parameters)}*");

        var time = DateTimeOffset.FromUnixTimeSeconds(signal.Time);
        builder.AppendLine($"Время свечки: *{time:g}*");

        var utcTime = DateTime.UtcNow;
        var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        var moscowTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, moscowTimeZone);
        builder.AppendLine($"Время сигнала: *{moscowTime:g}*");

        return builder.ToString();
    }
}