using System.ComponentModel;

namespace Bablomet.Common.Infrastructure;

public enum IndicatorType
{
    [Description("SMA")]
    SMA,
    
    [Description("EMA")]
    EMA,
    
    [Description("MACD")]
    MACD,
    
    [Description("VWAP и объемы")]
    VWAP
}
