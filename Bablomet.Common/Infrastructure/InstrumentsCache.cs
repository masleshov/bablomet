using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bablomet.Common.Domain;
using Bablomet.Common.Repository;

namespace Bablomet.Common.Infrastructure;

public static class InstrumentsCache
{
    private static ConcurrentDictionary<string, Instrument> _instruments = new();
    
    public static async Task Init(UnitOfWork uow)
    {
        if (uow == null)
        {
            throw new ArgumentNullException(nameof(uow));
        }

        var instruments = await uow.GetRepository<InstrumentRepository>().GetInstruments();
        foreach (var instrument in instruments)
        {
            _instruments.TryAdd(instrument.Symbol, instrument);
        }
    }

    public static Instrument GetInstrument(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new ArgumentNullException(nameof(ticker));
        }

        return _instruments.GetValueOrDefault(ticker);
    }

    public static Instrument[] GetAllInstruments()
    {
        return _instruments.Values.ToArray();
    }
}