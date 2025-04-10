using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Bablomet.PRO.Telegram.UI;
using Bablomet.PRO.Telegram.Constants;
using Bablomet.PRO.Telegram.Services;
using Bablomet.Common.Infrastructure.Extensions;
using Bablomet.Common.Infrastructure;

namespace Bablomet.PRO.Telegram.Handlers;

public class UpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly MenuGenerator _menuGenerator;
    private readonly TimeframeService _timeframeService;
    private readonly TickerService _tickerService;
    private readonly SignalSettingsService _signalSettingsService;

    private static readonly ConcurrentDictionary<long, IndicatorType> _chosenSignalType = new();
    private static readonly ConcurrentDictionary<long, string> _chosenTicker = new();
    private static readonly ConcurrentDictionary<long, string> _chosenTimeframe = new();
    private static readonly ConcurrentBag<long> _awaitingManualParameters = new();

    public UpdateHandler(
        ITelegramBotClient botClient,
        MenuGenerator menuGenerator,
        TimeframeService timeframeService,
        TickerService tickerService,
        SignalSettingsService signalSettingsService)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _menuGenerator = menuGenerator ?? throw new ArgumentNullException(nameof(menuGenerator));
        _timeframeService = timeframeService ?? throw new ArgumentNullException(nameof(timeframeService));
        _tickerService = tickerService ?? throw new ArgumentNullException(nameof(tickerService));
        _signalSettingsService = signalSettingsService ?? throw new ArgumentNullException(nameof(signalSettingsService));
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update?.Message?.Text is not { } messageText)
            return;

        var chatId = update.Message.Chat.Id;
        Console.WriteLine($"[LOG] Received message from {chatId}: {messageText}");

        if (_awaitingManualParameters.Contains(chatId))
        {
            await HandleManualParametersInput(chatId, messageText, cancellationToken);
            return;
        }

        switch (messageText)
        {
            case MenuCommands.Start:
            case MenuCommands.BackToMenu:
                ClearUserSelection(chatId);
                await botClient.SendMessage(
                    chatId,
                    "📊 *Bablomet PRO*\n\nДобро пожаловать в Bablomet PRO!\nВыберите нужный раздел:",
                    replyMarkup: _menuGenerator.GetMainMenu(),
                    cancellationToken: cancellationToken
                );
                return;

            case MenuCommands.TradingSignals:
                ClearUserSelection(chatId);
                await botClient.SendMessage(
                    chatId,
                    "🔹 Выберите нужный индикатор:",
                    replyMarkup: _menuGenerator.GetTradingSignalsMenu(),
                    cancellationToken: cancellationToken
                );
                return;
        }

        if (TryGetSignalTypeByCommand(messageText, out var selectedSignalType))
        {
            _chosenSignalType[chatId] = selectedSignalType;

            var instrumentMenu = await _menuGenerator.GetInstrumentSelectionMenuAsync();
            await botClient.SendMessage(
                chatId,
                $"📈 *Вы выбрали «{selectedSignalType.GetDescription()}»*\n\nВыберите торговый инструмент (акцию):",
                replyMarkup: instrumentMenu,
                cancellationToken: cancellationToken
            );
            return;
        }

        if (_chosenSignalType.TryGetValue(chatId, out var signalType) &&
            !_chosenTicker.ContainsKey(chatId))
        {
            var extractedTicker = ExtractTicker(messageText);
            if (string.IsNullOrWhiteSpace(extractedTicker) || !IsValidTicker(extractedTicker))
            {
                await botClient.SendMessage(chatId,
                    "❌ Неверный формат тикера. Введите тикер в формате `SBER`, `AAPL` или выберите из списка.",
                    cancellationToken: cancellationToken);
                return;
            }

            _chosenTicker[chatId] = extractedTicker;

            var timeframeMenu = await _menuGenerator.GetTimeframeSelectionMenuAsync(signalType.GetDescription(), extractedTicker);
            await botClient.SendMessage(
                chatId,
                $"🕑 Выберите таймфрейм для сигналов {signalType.GetDescription()} по акции {extractedTicker}:",
                replyMarkup: timeframeMenu,
                cancellationToken: cancellationToken
            );
            return;
        }

        var mapping = await _timeframeService.GetTimeframeMappingAsync();
        if (_chosenTicker.TryGetValue(chatId, out var ticker) &&
            _chosenSignalType.TryGetValue(chatId, out var storedSignalType) &&
            mapping.TryGetValue(messageText, out var rawTimeframe))
        {
            _chosenTimeframe[chatId] = rawTimeframe;

            var defaultParams = _signalSettingsService.GetDefaultParameters(storedSignalType);
            var defaultParamsString = string.Join(", ", defaultParams);

            await botClient.SendMessage(
                chatId,
                $"⚙️ Выберите параметры {storedSignalType.GetDescription()} для сигналов по {ticker} (таймфрейм: {messageText}):\n\nИспользовать стандартные настройки?",
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { $"{MenuCommands.UseDefaultSettings} ({defaultParamsString})" },
                    new KeyboardButton[] { MenuCommands.SetManualSettings },
                    new KeyboardButton[] { MenuCommands.BackToMenu }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = false
                },
                cancellationToken: cancellationToken
            );
            return;
        }

        if (messageText.StartsWith(MenuCommands.UseDefaultSettings))
        {
            var parameters = _signalSettingsService.GetDefaultParameters(_chosenSignalType[chatId])
                .Select(int.Parse).ToArray();

            await _signalSettingsService.SubscribeUserToIndicatorAsync(chatId,
                _chosenSignalType[chatId], _chosenTicker[chatId], _chosenTimeframe[chatId], parameters);

            await botClient.SendMessage(chatId, "✅ Вы успешно подписаны!",
                replyMarkup: _menuGenerator.GetMainMenu(), cancellationToken: cancellationToken);
            return;
        }

        if (messageText == MenuCommands.SetManualSettings)
        {
            var exampleParams = _signalSettingsService.GetDefaultParameters(_chosenSignalType[chatId]);
            var exampleString = string.Join(", ", exampleParams);

            _awaitingManualParameters.Add(chatId);

            await botClient.SendMessage(
                chatId,
                $"Введите параметры для сигнала {_chosenSignalType[chatId].GetDescription()} через запятую.\nНапример: {exampleString}",
                cancellationToken: cancellationToken
            );
            return;
        }

        await botClient.SendMessage(
            chatId,
            "Я не понимаю эту команду. Используйте меню ниже.",
            replyMarkup: _menuGenerator.GetMainMenu(),
            cancellationToken: cancellationToken
        );
    }

    private static string ExtractTicker(string input)
    {
        var trimmed = input.Trim();
        var parts = trimmed.Split(' ', '(', ')');
        return parts.FirstOrDefault(p => IsValidTicker(p)) ?? string.Empty;
    }

    private static bool IsValidTicker(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.All(char.IsUpper) &&
               value.All(char.IsLetter);
    }

    private async Task HandleManualParametersInput(long chatId, string input, CancellationToken cancellationToken)
    {
        if (!_chosenSignalType.TryGetValue(chatId, out var signalType) ||
            !_chosenTicker.TryGetValue(chatId, out var ticker) ||
            !_chosenTimeframe.TryGetValue(chatId, out var timeframe))
        {
            await _botClient.SendMessage(chatId, "⚠️ Произошла ошибка. Пожалуйста, начните заново.", cancellationToken: cancellationToken);
            ClearUserSelection(chatId);
            return;
        }

        var isValid = _signalSettingsService.ValidateUserParameters(signalType, input);
        if (!isValid)
        {
            var exampleParams = _signalSettingsService.GetDefaultParameters(signalType);
            var exampleString = string.Join(", ", exampleParams);
            await _botClient.SendMessage(
                chatId,
                $"❌ Неверный формат параметров.\nНапример: {exampleString}",
                cancellationToken: cancellationToken
            );
            return;
        }

        var parameters = input.Split(',').Select(p => int.Parse(p.Trim())).ToArray();

        await _signalSettingsService.SubscribeUserToIndicatorAsync(chatId, signalType, ticker, timeframe, parameters);

        _awaitingManualParameters.TryTake(out chatId);

        await _botClient.SendMessage(
            chatId,
            $"✅ Приняты настройки {signalType.GetDescription()} ({input}) по {ticker} на таймфрейме {TimeframeService.ToUserFriendly(timeframe)}.\n\nСигналы настроены!",
            replyMarkup: _menuGenerator.GetMainMenu(),
            cancellationToken: cancellationToken
        );

        ClearUserSelection(chatId);
    }

    private void ClearUserSelection(long chatId)
    {
        _chosenSignalType.TryRemove(chatId, out _);
        _chosenTicker.TryRemove(chatId, out _);
        _chosenTimeframe.TryRemove(chatId, out _);
        _awaitingManualParameters.TryTake(out chatId);
    }

    private bool TryGetSignalTypeByCommand(string command, out IndicatorType signalType)
    {
        return command switch
        {
            MenuCommands.SMA => (signalType = IndicatorType.SMA) == signalType,
            MenuCommands.EMA => (signalType = IndicatorType.EMA) == signalType,
            MenuCommands.MACD => (signalType = IndicatorType.MACD) == signalType,
            MenuCommands.VWAP => (signalType = IndicatorType.VWAP) == signalType,
            _ => (signalType = default) != signalType
        };
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[ERROR] {exception.Message}");
        return Task.CompletedTask;
    }
}