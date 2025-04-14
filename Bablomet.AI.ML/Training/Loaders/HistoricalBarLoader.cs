using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Bablomet.Common.Domain;
using Bablomet.Common.Infrastructure;
using Bablomet.Common.Internal;
using Microsoft.Extensions.Logging;

namespace Bablomet.AI.ML.Training.Loaders;

public class HistoricalBarLoader
{
    private readonly IMarketDataClient _client;
    private readonly ILogger<HistoricalBarLoader> _logger;

    private static readonly Dictionary<string, TimeSpan> _timeFrameIntervals = new()
    {
        [TimeFrames.Minute] = TimeSpan.FromHours(1),       // 1m → 1 час
        [TimeFrames.Minutes5] = TimeSpan.FromHours(4),       // 5m → 4 часа
        [TimeFrames.Minutes15] = TimeSpan.FromHours(8),      // 15m → 8 часов
        [TimeFrames.Minutes60] = TimeSpan.FromDays(1),       // 1h → 1 день
        [TimeFrames.Days] = TimeSpan.FromDays(10),       // 1d → 10 дней
        [TimeFrames.Weeks] = TimeSpan.FromDays(30),       // 1w → 30 дней
        [TimeFrames.Months] = TimeSpan.FromDays(90)        // 1mo → 3 месяца
    };

    public HistoricalBarLoader(IMarketDataClient client, ILogger<HistoricalBarLoader> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LoadAndCacheBarsAsync(
        string symbol, 
        string exchange, 
        string tf, 
        string path, 
        DateTimeOffset from,
        BufferBlock<Bar[]> barQueue
    )
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentNullException(nameof(symbol));
        if (string.IsNullOrWhiteSpace(exchange)) throw new ArgumentNullException(nameof(exchange));
        if (string.IsNullOrWhiteSpace(tf)) throw new ArgumentNullException(nameof(tf));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        if (barQueue == null) throw new ArgumentNullException(nameof(barQueue));

        if (!_timeFrameIntervals.TryGetValue(tf, out var chunkSize))
        {
            throw new ArgumentException($"Unsupported timeframe: {tf}", nameof(tf));
        }

        var allBars = new List<Bar>();
        var now = DateTimeOffset.UtcNow;
        if (from >= now) throw new ArgumentException($"{nameof(from)} can't be greatest or equal DateTimeOffset.UtcNow!");

        var to = from + chunkSize;

        while (from < now)
        {
            try
            {
                var bars = await _client.GetBarsHistory(symbol, exchange, tf, from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds());
                if (bars == null || bars.Length == 0) break;

                await barQueue.SendAsync(bars);

                allBars.AddRange(bars);
                _logger.LogDebug("Loaded {Count} bars for {Symbol} {Tf} from {From:g} to {To:g}", bars.Length, symbol, tf, from, to);

                from = to.AddSeconds(1);
                to = from + chunkSize;

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(allBars));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading bars for {Symbol} {Tf} from {From:g} to {To:g}", symbol, tf, from, to);
                break;
            }
        }

        _logger.LogInformation("Saved {Count} bars for {Symbol} {Tf} to {Path}", allBars.Count, symbol, tf, path);
    }
}