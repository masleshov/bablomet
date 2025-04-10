namespace Bablomet.API.Calculators;

using System;
using System.Collections.Generic;
using Bablomet.Common.Domain;

public class AverageDirectionalIndexCalculator
{
    private readonly int _period;
    private readonly Queue<Bar> _bars = new Queue<Bar>();
    private decimal _previousHigh;
    private decimal _previousLow;
    private decimal _previousClose;

    private decimal _adx;
    private decimal _trSum;
    private decimal _plusDmSum;
    private decimal _minusDmSum;

    public AverageDirectionalIndexCalculator(int period = 14)
    {
        if (period <= 0) throw new ArgumentException("Period must be greater than 0", nameof(period));

        _period = period;
    }

    public decimal? Calculate(Bar bar)
    {
        if (_bars.Count == 0)
        {
            // Initialize with the first bar
            _previousHigh = bar.High;
            _previousLow = bar.Low;
            _previousClose = bar.Close;
            _bars.Enqueue(bar);
            return null; // ADX needs more data to start calculation
        }

        // Calculate True Range (TR), +DM, and -DM
        var tr = Math.Max(bar.High - bar.Low, Math.Max(Math.Abs(bar.High - _previousClose), Math.Abs(bar.Low - _previousClose)));
        var plusDm = bar.High > _previousHigh ? Math.Max(bar.High - _previousHigh, 0) : 0;
        var minusDm = bar.Low < _previousLow ? Math.Max(_previousLow - bar.Low, 0) : 0;

        _trSum += tr;
        _plusDmSum += plusDm;
        _minusDmSum += minusDm;

        if (_bars.Count > _period)
        {
            var oldestBar = _bars.Dequeue();
            var oldestTr = Math.Max(oldestBar.High - oldestBar.Low, Math.Max(Math.Abs(oldestBar.High - _previousClose), Math.Abs(oldestBar.Low - _previousClose)));
            _trSum -= oldestTr;

            var oldestPlusDm = oldestBar.High > _previousHigh ? Math.Max(oldestBar.High - _previousHigh, 0) : 0;
            var oldestMinusDm = oldestBar.Low < _previousLow ? Math.Max(_previousLow - oldestBar.Low, 0) : 0;
            _plusDmSum -= oldestPlusDm;
            _minusDmSum -= oldestMinusDm;
        }

        var plusDi = 100 * (_plusDmSum / _trSum);
        var minusDi = 100 * (_minusDmSum / _trSum);

        var dx = Math.Abs(plusDi - minusDi) / (plusDi + minusDi) * 100;

        if (_bars.Count == _period)
        {
            _adx = dx; // Initial ADX value
        }
        else if (_bars.Count > _period)
        {
            _adx = ((_adx * (_period - 1)) + dx) / _period; // Smooth ADX value
        }

        _previousHigh = bar.High;
        _previousLow = bar.Low;
        _previousClose = bar.Close;
        _bars.Enqueue(bar);

        return _adx;
    }
}