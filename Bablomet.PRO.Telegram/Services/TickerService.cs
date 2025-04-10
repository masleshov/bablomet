namespace Bablomet.PRO.Telegram.Services;

public class TickerService
{
    public Task<List<string>> GetPopularTickersAsync(int limit = 5)
    {
        var tickers = new List<string>
        {
            "SBER (Сбербанк)", "GAZP (Газпром)", "YNDX (Яндекс)", "AAPL (Apple)", "TSLA (Tesla)",
            "MSFT (Microsoft)", "AMZN (Amazon)", "GOOGL (Google)", "NFLX (Netflix)", "META (Meta)"
        };

        return Task.FromResult(tickers.Take(limit).ToList());
    }
}