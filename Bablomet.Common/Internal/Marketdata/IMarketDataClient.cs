using System.Threading.Tasks;
using Bablomet.Common.Domain;
using Refit;

namespace Bablomet.Common.Internal;

public interface IMarketDataClient
{
    [Get("/api/history")]
    Task<Bar[]> GetBarsHistory([Query] string symbol, string exchange, string tf, long from, long to);
}