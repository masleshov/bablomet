using System.Text.Json.Serialization;

namespace Bablomet.Marketdata.External;

public class BarsHistoryResponseDto
{
    [JsonPropertyName("history")]
    public BarHistoryResponseDto[] History { get; set; }
    
    [JsonPropertyName("next")]
    public long Next { get; set; }
    
    [JsonPropertyName("prev")]
    public long Prev { get; set; }
}