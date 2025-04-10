using System.Text.Json.Serialization;

namespace Bablomet.Marketdata.External;

public class BarHistoryResponseDto
{
    [JsonPropertyName("time")]
    public long Time { get; set; }
    
    [JsonPropertyName("close")]
    public decimal Close { get; set; }
    
    [JsonPropertyName("open")]
    public decimal Open { get; set; }
    
    [JsonPropertyName("high")]
    public decimal High { get; set; }
    
    [JsonPropertyName("Low")]
    public decimal Low { get; set; }
    
    [JsonPropertyName("volume")]
    public long Volume { get; set; }
}