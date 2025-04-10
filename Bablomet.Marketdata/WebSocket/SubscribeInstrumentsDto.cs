using System;
using System.Text.Json.Serialization;
using Bablomet.Marketdata.Infrastructure;

namespace Bablomet.Marketdata.WebSocket;

public sealed class SubscribeInstrumentsDto
{
    [JsonPropertyName("opcode")]
    public string OperationCode { get; set; } = "InstrumentsGetAndSubscribeV2";

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
}