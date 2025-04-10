using Telegram.Bot;

namespace Bablomet.PRO.Telegram.Services;

internal static class CommandProcessor
{
    public static Task<string> ProcessCommandAsync(ITelegramBotClient botClient, long chatId, string command, CancellationToken cancellationToken)
    {
        return Task.FromResult(command switch
        {
            "/start" => "Добро пожаловать в Bablomet PRO! 🚀",
            "/help" => "Доступные команды:\n/start - Начало работы\n/help - Список команд\n/settings - Настройки",
            "/settings" => "Выберите настройки:",
            _ => "Я не понимаю эту команду. Используйте меню команд ниже."
        });
    }
}