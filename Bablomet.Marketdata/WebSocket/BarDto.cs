using System;
using System.Text.Json.Serialization;

namespace Bablomet.Marketdata.WebSocket;

public sealed class BarDto
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; set; }

    [JsonPropertyName("data")]
    public BarDataDto Data { get; set; }
}

public sealed class BarDataDto
{
    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("close")]
    public decimal Close { get; set; }

    [JsonPropertyName("open")]
    public decimal Open { get; set; }

    [JsonPropertyName("high")]
    public decimal High { get; set; }

    [JsonPropertyName("low")]
    public decimal Low { get; set; }

    [JsonPropertyName("volume")]
    public long Volume { get; set; }
}