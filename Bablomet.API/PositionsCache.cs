using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bablomet.API.Domain;
using Bablomet.Common.Domain;

namespace Bablomet.API;

internal static class PositionsCache
{
    private static readonly ConcurrentDictionary<string, Portfolio> _portfolios = new();

    public static void InitPortfolio(string portfolioCode, Dictionary<string, decimal> money)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }

        _portfolios.TryAdd(portfolioCode, new Portfolio { Code = portfolioCode});

        if (money == null || money.Count == 0)
        {
            return;
        }

        foreach (var kvp in money)
        {
            AddMoney(portfolioCode, kvp.Key, kvp.Value);
        }
    }

    public static decimal GetMoney(string portfolioCode, string currencyTicker)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (string.IsNullOrWhiteSpace(currencyTicker))
        {
            throw new ArgumentNullException(nameof(currencyTicker));
        }

        var position = GetPosition(portfolioCode, currencyTicker);
        return position?.TotalQuantity ?? 0;
    }

    public static decimal AddMoney(string portfolioCode, string currencyTicker, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (string.IsNullOrWhiteSpace(currencyTicker))
        {
            throw new ArgumentNullException(nameof(currencyTicker));
        }

        var position = GetOrCreatePosition(portfolioCode, currencyTicker);
        var move = new Move
        {
            Quantity = amount,
            Price = 1,
            Direction = MoveDirection.Buy
        };
        position.Moves.Add(DateTimeOffset.UtcNow, move);
        
        position.TotalQuantity += move.Quantity;
        position.AveragePrice = 1;
        return position.TotalQuantity;
    }
    
    public static void SubtractMoney(string portfolioCode, string currencyTicker, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (string.IsNullOrWhiteSpace(currencyTicker))
        {
            throw new ArgumentNullException(nameof(currencyTicker));
        }

        var position = GetOrCreatePosition(portfolioCode, currencyTicker);
        if (position.TotalQuantity < amount)
        {
            throw new InvalidOperationException($"Total quantity {position.TotalQuantity} is less than {amount}");
        }
        
        var move = new Move
        {
            Quantity = amount,
            Price = 1,
            Direction = MoveDirection.Sell
        };
        position.Moves.Add(DateTimeOffset.UtcNow, move);
        
        position.TotalQuantity -= move.Quantity;
        position.AveragePrice = 1;
    }

    public static (decimal, decimal) OpenOrAddPosition(string portfolioCode, Instrument instrument, decimal quantity, decimal price)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (instrument == null)
        {
            throw new ArgumentNullException(nameof(instrument));
        }

        if (!_portfolios.TryGetValue(portfolioCode, out var portfolio))
        {
            throw new NullReferenceException($"No portfolio found with code {portfolioCode}");
        }
        
        // If the position already exists, we just add the amount to it
        var now = DateTimeOffset.UtcNow;
        var position = GetOrCreatePosition(portfolioCode, instrument.Symbol);
        var move = new Move
        {
            Quantity = quantity,
            Price = price,
            Direction = MoveDirection.Buy
        };
        position.Moves.Add(now, move);
        
        AddPositionTotalQuantityAndAveragePrice(position, move);
        SubtractMoney(portfolioCode, instrument.Currency, move.Quantity * price);

        return (position.TotalQuantity, position.AveragePrice);
    }
    
    public static (decimal, decimal) ClosePosition(string portfolioCode, Instrument instrument, decimal price)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (instrument == null)
        {
            throw new ArgumentNullException(nameof(instrument));
        }

        var position = GetPosition(portfolioCode, instrument.Symbol);
        if (position == null)
        {
            throw new InvalidOperationException($"Position by {instrument.Symbol} doesn't exist");
        }
        
        var move = new Move
        {
            Quantity = position.TotalQuantity,
            Price = price,
            Direction = MoveDirection.Sell
        };
        position.Moves.Add(DateTimeOffset.UtcNow, move);
        AddMoney(portfolioCode, instrument.Currency, move.Quantity * move.Price);
        
        var pl = (price - position.AveragePrice) * position.TotalQuantity;
        var totalQuantity = position.TotalQuantity;

        position.TotalQuantity = 0;
        position.AveragePrice = 0;
        
        position.ClosedAt = DateTimeOffset.UtcNow;
        return (totalQuantity, pl);
    }
    
    public static Position GetPosition(string portfolioCode, string ticker)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new ArgumentNullException(nameof(ticker));
        }

        if (!_portfolios.TryGetValue(portfolioCode, out var portfolio))
        {
            throw new NullReferenceException($"No portfolio found with code {portfolioCode}");
        }
        
        // Suppose that positions are ordered
        if (!portfolio.Positions.TryGetValue(ticker, out var positions))
        {
            positions = new List<Position>();
            portfolio.Positions.Add(ticker, positions);
        }
        
        return positions.FirstOrDefault(position => !position.ClosedAt.HasValue);
    }

    public static void PrintPortfolio(string portfolioCode)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (!_portfolios.TryGetValue(portfolioCode, out var portfolio))
        {
            throw new NullReferenceException($"No portfolio found with code {portfolioCode}");
        }

        Console.WriteLine($"---------{portfolioCode}---------");
        foreach (var position in portfolio.Positions
                .SelectMany(kvp => kvp.Value
                .Where(position => !position.ClosedAt.HasValue)))
        {
            Console.WriteLine($"{position.Symbol} | TotalQuantity: {position.TotalQuantity} ::: AveragePrice {position.AveragePrice}");
        }
        Console.WriteLine($"-------------------");
    }

    private static Position GetOrCreatePosition(string portfolioCode, string ticker)
    {
        if (string.IsNullOrWhiteSpace(portfolioCode))
        {
            throw new ArgumentNullException(nameof(portfolioCode));
        }
        
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new ArgumentNullException(nameof(ticker));
        }

        if (!_portfolios.TryGetValue(portfolioCode, out var portfolio))
        {
            throw new NullReferenceException($"No portfolio found with code {portfolioCode}");
        }
        
        // Suppose that positions are ordered
        if (!portfolio.Positions.TryGetValue(ticker, out var positions))
        {
            positions = new List<Position>();
            portfolio.Positions.Add(ticker, positions);
        }
        
        var now = DateTimeOffset.UtcNow;
        var position = positions.FirstOrDefault(position => !position.ClosedAt.HasValue);
        if (position == null)
        {
            position = new Position
            {
                Symbol = ticker,
                OpenedAt = now,
                Moves = new SortedList<DateTimeOffset, Move>()
            };
            positions.Insert(0, position);
        }

        return position;
    }

    private static void AddPositionTotalQuantityAndAveragePrice(Position position, Move move)
    {
        if (position == null)
        {
            throw new ArgumentNullException(nameof(position));
        }

        if (move == null)
        {
            throw new ArgumentNullException(nameof(move));
        }
        
        var totalCostBefore = position.AveragePrice * position.TotalQuantity;
        var costOfNewBuy = move.Price * move.Quantity;
        position.TotalQuantity += move.Quantity;
        position.AveragePrice = (totalCostBefore + costOfNewBuy) / position.TotalQuantity;
    }
}