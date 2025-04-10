using Bablomet.Common.Infrastructure;

namespace Bablomet.PRO.Telegram.Services
{
    public class TimeframeService
    {
        public async Task<List<string>> GetAvailableTimeframesAsync()
        {
            // Заглушка для получения таймфреймов из внешнего сервиса
            await Task.Delay(10);
            var rawTimeframes = new List<string>
            {
                TimeFrames.Minute,
                TimeFrames.Minutes5,
                TimeFrames.Minutes15,
                TimeFrames.Minutes60,
                TimeFrames.Days,
                TimeFrames.Weeks,
                TimeFrames.Months,
                TimeFrames.Years
            };
            return rawTimeframes;
        }

        public static string ToUserFriendly(string rawTimeframe)
        {
            return rawTimeframe switch
            {
                TimeFrames.Minute => "1 минута",
                TimeFrames.Minutes5 => "5 минут",
                TimeFrames.Minutes15 => "15 минут",
                TimeFrames.Minutes60 => "1 час",
                TimeFrames.Days => "1 день",
                TimeFrames.Weeks => "1 неделя",
                TimeFrames.Months => "1 месяц",
                TimeFrames.Years => "1 год",
                _ => $"Неизвестный таймфрейм ({rawTimeframe})"
            };
        }

        public async Task<Dictionary<string, string>> GetTimeframeMappingAsync()
        {
            var rawTimeframes = await GetAvailableTimeframesAsync();
            var mapping = new Dictionary<string, string>();
            foreach (var raw in rawTimeframes)
            {
                mapping[ToUserFriendly(raw)] = raw;
            }
            return mapping;
        }
    }
}