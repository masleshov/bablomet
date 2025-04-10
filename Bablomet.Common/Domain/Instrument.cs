namespace Bablomet.Common.Domain;

public sealed class Instrument
{
    public string Symbol { get; set; }
    public string Shortname { get; set; }
    public string Description { get; set; }
    public string Exchange { get; set; }
    public string Type { get; set; }
    public int LotSize { get; set; }
    public decimal FaceValue { get; set; }
    public string CfiCode { get; set; }
    public long Cancellation { get; set; }
    public decimal MinStep { get; set; }
    public long Rating { get; set; }
    public decimal MarginBuy { get; set; }
    public decimal MarginSell { get; set; }
    public decimal MarginRate { get; set; }
    public decimal PriceStep { get; set; }
    public decimal PriceMax { get; set; }
    public decimal PriceMin { get; set; }
    public decimal TheorPrice { get; set; }
    public decimal TheorPriceLimit { get; set; }
    public long Volatility { get; set; }
    public string Currency { get; set; }
    public string ISIN { get; set; }
    public decimal? Yield { get; set; }
    public string PrimaryBoard { get; set; }
    public int TradingStatus { get; set; }
    public string TradingStatusInfo { get; set; }
    public string ComplexProductCategory { get; set; }
}