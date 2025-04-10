using Telegram.Bot.Types.ReplyMarkups;
using Bablomet.PRO.Telegram.Constants;
using Bablomet.PRO.Telegram.Services;

namespace Bablomet.PRO.Telegram.UI
{
    public class MenuGenerator
    {
        private readonly TickerService _tickerService;
        private readonly TimeframeService _timeframeService;

        public MenuGenerator(TickerService tickerService, TimeframeService timeframeService)
        {
            _tickerService = tickerService ?? throw new ArgumentNullException(nameof(tickerService));
            _timeframeService = timeframeService ?? throw new ArgumentNullException(nameof(timeframeService));
        }

        public ReplyKeyboardMarkup GetMainMenu()
        {
            var keyboard = new[]
            {
                new KeyboardButton[] { MenuCommands.TradingSignals, MenuCommands.Settings },
                new KeyboardButton[] { MenuCommands.Information }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        public ReplyKeyboardMarkup GetTradingSignalsMenu()
        {
            var keyboard = new[]
            {
                new KeyboardButton[] { MenuCommands.SMA, MenuCommands.EMA },
                new KeyboardButton[] { MenuCommands.MACD, MenuCommands.VWAP },
                new KeyboardButton[] { MenuCommands.BackToMenu }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        public async Task<ReplyKeyboardMarkup> GetInstrumentSelectionMenuAsync()
        {
            var tickers = await _tickerService.GetPopularTickersAsync(5);
            // Отображаем тикеры без внутренних префиксов
            var keyboard = tickers
                .Select(ticker => new KeyboardButton(ticker))
                .Chunk(2)
                .Select(chunk => chunk.ToArray())
                .ToList();

            keyboard.Add(new[] { new KeyboardButton(MenuCommands.BackToMenu) });

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        public async Task<ReplyKeyboardMarkup> GetTimeframeSelectionMenuAsync(string signalType, string ticker)
        {
            // Получаем mapping: user-friendly → raw
            var mapping = await _timeframeService.GetTimeframeMappingAsync();
            // Формируем кнопки, отображая человекочитаемые значения
            var keyboard = mapping.Keys
                .Select(userFriendly => new KeyboardButton(userFriendly))
                .Chunk(2)
                .Select(chunk => chunk.ToArray())
                .ToList();

            keyboard.Add(new[] { new KeyboardButton(MenuCommands.BackToMenu) });

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        public ReplyKeyboardMarkup GetSettingsMenu()
        {
            var keyboard = new[]
            {
                new KeyboardButton[] { MenuCommands.IndicatorSettings, MenuCommands.NotificationFrequency },
                new KeyboardButton[] { MenuCommands.ToggleSignals },
                new KeyboardButton[] { MenuCommands.BackToMenu }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        public ReplyKeyboardMarkup GetInformationMenu()
        {
            var keyboard = new[]
            {
                new KeyboardButton[] { MenuCommands.WhatIsBablomet, MenuCommands.HowToUse },
                new KeyboardButton[] { MenuCommands.Help },
                new KeyboardButton[] { MenuCommands.BackToMenu }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
    }
}