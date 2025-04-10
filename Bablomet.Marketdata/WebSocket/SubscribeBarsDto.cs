using System;
using System.Text.Json.Serialization;
using Bablomet.Marketdata.Infrastructure;

namespace Bablomet.Marketdata.WebSocket;

public sealed class SubscribeBarsDto
{

    [JsonPropertyName("opcode")]
    public string OperationCode { get; set; } = "BarsGetAndSubscribe";

    [JsonPropertyName("guid")]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [JsonPropertyName("token")]
    public string Token { get; set; } = AlorJwtHolder.Jwt;

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("exchange")]
    public string Exchange { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; } = "Simple";

    [JsonPropertyName("tf")]
    public string TimeFrame { get; set; }

    [JsonPropertyName("from")]
    public long From { get; set; }

    [JsonPropertyName("delayed")]
    public bool Delayed { get; set; } = false;

    [JsonPropertyName("frequency")]
    public int Frequency { get; set; } = 175;
}
