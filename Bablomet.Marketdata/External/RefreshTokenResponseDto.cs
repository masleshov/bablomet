using System.Text.Json.Serialization;

namespace Bablomet.Marketdata.External;

public sealed class RefreshTokenResponseDto
{
    [JsonPropertyName("AccessToken")]
    public string AccessToken { get; set; }
}