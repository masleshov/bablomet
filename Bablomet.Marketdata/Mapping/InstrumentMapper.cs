using System;
using Bablomet.Common.Domain;
using Bablomet.Marketdata.External;
using Riok.Mapperly.Abstractions;

namespace Bablomet.Marketdata.Mapping;

[Mapper]
public static partial class InstrumentMapper
{
    public static Instrument ToDomain(InstrumentResponseDto source)
    {
        var instrument = ToDomainCore(source);
        instrument.Cancellation = ((DateTimeOffset)source.Cancellation).ToUnixTimeSeconds();
        return instrument;
    }
    
    [MapperIgnoreTarget(nameof(Instrument.Cancellation))]
    private static partial Instrument ToDomainCore(InstrumentResponseDto source);
}