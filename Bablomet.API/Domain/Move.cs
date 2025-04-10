using System;

namespace Bablomet.API.Domain;

public class Move
{
    public long MoveId { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public MoveDirection Direction { get; set; }
    public DateTimeOffset DateTime { get; set; }
}