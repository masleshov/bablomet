using System;
using System.Text.Json.Serialization;

namespace Bablomet.Marketdata.External;

public sealed class InstrumentResponseDto
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("shortname")]
    public string Shortname { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("exchange")]
    public string Exchange { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("lotsize")]
    public int LotSize { get; set; }

    [JsonPropertyName("facevalue")]
    public decimal FaceValue { get; set; }

    [JsonPropertyName("cfiCode")]
    public string CfiCode { get; set; }

    [JsonPropertyName("cancellation")]
    public DateTime Cancellation { get; set; }

    [JsonPropertyName("minstep")]
    public decimal MinStep { get; set; }

    [JsonPropertyName("rating")]
    public long Rating { get; set; }

    [JsonPropertyName("marginbuy")]
    public decimal MarginBuy { get; set; }

    [JsonPropertyName("marginsell")]
    public decimal MarginSell { get; set; }

    [JsonPropertyName("marginrate")]
    public decimal MarginRate { get; set; }

    [JsonPropertyName("pricestep")]
    public decimal PriceStep { get; set; }

    [JsonPropertyName("priceMax")]
    public decimal PriceMax { get; set; }

    [JsonPropertyName("priceMin")]
    public decimal PriceMin { get; set; }

    [JsonPropertyName("theorPrice")]
    public decimal TheorPrice { get; set; }

    [JsonPropertyName("theorPriceLimit")]
    public decimal TheorPriceLimit { get; set; }

    [JsonPropertyName("volatility")]
    public decimal Volatility { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("ISIN")]
    public string ISIN { get; set; }

    [JsonPropertyName("yield")]
    public decimal? Yield { get; set; }

    [JsonPropertyName("primary_board")]
    public string PrimaryBoard { get; set; }

    [JsonPropertyName("tradingStatus")]
    public int TradingStatus { get; set; }

    [JsonPropertyName("tradingStatusInfo")]
    public string TradingStatusInfo { get; set; }

    [JsonPropertyName("complexProductCategory")]
    public string ComplexProductCategory { get; set; }
}