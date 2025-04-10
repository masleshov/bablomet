using System;
using System.Collections.Generic;

namespace Bablomet.API.Domain;

public class Position
{
    public int PositionId { get; set; }
    public string Symbol { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AveragePrice { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    
    public SortedList<DateTimeOffset, Move> Moves { get; set; }
}