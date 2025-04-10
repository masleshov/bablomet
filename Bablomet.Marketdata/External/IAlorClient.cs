using System.Threading.Tasks;
using Refit;

namespace Bablomet.Marketdata.External;

public interface IAlorClient
{
    [Get("/md/v2/Securities")]
    Task<InstrumentResponseDto[]> GetInstruments(string exchange, string sector, string instrumentGroup, int limit, int offset);
    
    [Get("/md/v2/history")]
    Task<BarsHistoryResponseDto> GetBarsHistory([Query] string symbol, string exchange, string tf, long from, long to);
}