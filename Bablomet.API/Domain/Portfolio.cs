using System.Collections.Generic;

namespace Bablomet.API.Domain;

public class Portfolio
{
    public int PortfolioId { get; set; }
    public string Code { get; set; }
    public Dictionary<string, List<Position>> Positions { get; set; } = new();
}