using System;
using System.Collections.Generic;
using Bablomet.API.Domain;
using Bablomet.API.Infrastructure;
using Bablomet.Common.Domain;

namespace Bablomet.API;

public class Trader
{
    private long _currentDelay;
    private long _possibleDelay;

    private readonly object _syncRoot = new();
    private readonly HashSet<string> _strategies;
    private readonly LinkedList<Bubble> _bubbles;
    
    public Trader(long possibleDelay, params string[] expectedStrategies)
    {
        if (expectedStrategies == null || expectedStrategies.Length == 0)
        {
            throw new ArgumentNullException(nameof(expectedStrategies));
        }

        if (possibleDelay <= 0)
        {
            throw new ArgumentNullException(nameof(possibleDelay));
        }

        _possibleDelay = possibleDelay;
        _strategies = new HashSet<string>(expectedStrategies);
        _bubbles = new LinkedList<Bubble>();
    }
    
    public void ReceiveSignal(string strategy, Signal signal)
    {
        if (string.IsNullOrWhiteSpace(strategy))
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        if (signal == null)
        {
            throw new ArgumentNullException(nameof(signal));
        }
        
        if (!_strategies.Contains(strategy))
        {
            throw new ArgumentException($"Unexpected strategy {strategy} received");
        }
        
        lock (_syncRoot)
        {
            foreach (var bubble in _bubbles)
            {
                if (bubble.Strategy != strategy)
                {
                    continue;
                }
                
                _bubbles.Remove(bubble);
                Console.WriteLine($"Removed a duplicate bubble {bubble.Strategy}");
            }
            
            if (_bubbles.Count == 0)
            {
                _bubbles.AddLast(new Bubble
                {
                    Strategy = strategy,
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                Console.WriteLine($"Added a new bubble {_bubbles.Last.Value.Strategy}, signal {signal.Instrument.Symbol}::{signal.Direction}");
                return;
            }

            var first = _bubbles.First.Value;
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - first.Time > _possibleDelay)
            {
                _bubbles.Clear();
                
                _bubbles.AddLast(new Bubble
                {
                    Strategy = strategy,
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                Console.WriteLine($"Destroyed a bubble, added a new bubble {_bubbles.Last.Value.Strategy}, signal {signal.Instrument.Symbol}::{signal.Direction}");
                return;
            }
            
            if (_bubbles.Count + 1 < _strategies.Count)
            {
                _bubbles.AddLast(new Bubble
                {
                    Strategy = strategy,
                    Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                Console.WriteLine($"Added a new bubble {_bubbles.Last.Value.Strategy}, signal {signal.Instrument.Symbol}::{signal.Direction}");
                return;
            }
            
            //success!
            _bubbles.Clear();
            Console.WriteLine($"Received a consolidated signal {signal.Instrument.Symbol}::{signal.Direction}!");
        }

        if (signal.Direction == MoveDirection.Buy)
        {
            Buy(signal.PortfolioCode, signal.Instrument, signal.Quantity, signal.Price);
        }
        else if (signal.Direction == MoveDirection.Sell)
        {
            SellAll(signal.PortfolioCode, signal.Instrument, signal.Price);
        }
    }
    
    private void Buy(string portfolioCode, Instrument instrument, decimal quantity, decimal price)
    {
        var (totalQuantity, averagePrice) = PositionsCache.OpenOrAddPosition(
            portfolioCode: portfolioCode,
            instrument: instrument,
            quantity: quantity, 
            price: price
        );
        
        Console.WriteLine($"BUY: {instrument.Symbol} - {quantity}:::{averagePrice}");
        PositionsCache.PrintPortfolio(portfolioCode);
    }

    private void SellAll(string portfolioCode, Instrument instrument, decimal price)
    {
        var (totalQuantity, pl) = PositionsCache.ClosePosition(
            portfolioCode: portfolioCode,
            instrument: instrument, 
            price: price
        );

        Console.WriteLine($"CLOSE: {instrument.Symbol} - {totalQuantity}:::{price} | P\\L = {pl}");
        PositionsCache.PrintPortfolio(portfolioCode);
    }

    private class Bubble
    {
        public long Time;
        public string Strategy;
    }
}