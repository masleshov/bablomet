using System;
using System.Text.Json.Serialization;

namespace Bablomet.Marketdata.WebSocket;

public class InstrumentUpdateDto
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; set; }

    [JsonPropertyName("data")]
    public InstrumentUpdateDataDto Data { get; set; }
}

public class InstrumentUpdateDataDto
{
    [JsonPropertyName("priceMax")]
    public decimal PriceMax { get; set; }

    [JsonPropertyName("priceMin")]
    public decimal PriceMin { get; set; }

    [JsonPropertyName("marginBuy")]
    public decimal MarginBuy { get; set; }

    [JsonPropertyName("marginSell")]
    public decimal MarginSell { get; set; }

    [JsonPropertyName("tradingStatus")]
    public int TradingStatus { get; set; }

    [JsonPropertyName("tradingStatusInfo")]
    public string TradingStatusInfo { get; set; }

    [JsonPropertyName("theorPrice")]
    public decimal TheorPrice { get; set; }

    [JsonPropertyName("theorPriceLimit")]
    public decimal TheorPriceLimit { get; set; }

    [JsonPropertyName("volatility")]
    public decimal Volatility { get; set; }
}