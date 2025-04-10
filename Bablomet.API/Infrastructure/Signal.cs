using Bablomet.API.Domain;
using Bablomet.Common.Domain;

namespace Bablomet.API.Infrastructure;

public class Signal
{
    public MoveDirection Direction { get; set; }
    public string PortfolioCode { get; set; }
    public Instrument Instrument { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}