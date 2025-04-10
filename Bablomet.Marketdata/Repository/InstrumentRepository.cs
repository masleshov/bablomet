using System;
using System.Threading.Tasks;
using Bablomet.Common.Domain;
using Bablomet.Common.Repository;
using Bablomet.Marketdata.WebSocket;
using Dapper;
using Npgsql;

namespace Bablomet.Marketdata.Repository;

public sealed class InstrumentRepository : BaseRepository
{
    public InstrumentRepository(NpgsqlConnection connection) : base(connection)
    {
    }

    public async Task AddInstrumentIfNotExists(Instrument instrument)
    {
        if (instrument == null) throw new ArgumentNullException(nameof(instrument));
        if (string.IsNullOrWhiteSpace(instrument.Symbol)) throw new ArgumentNullException(nameof(instrument.Symbol));

        var query =
            "insert into instruments (symbol, shortname, description, exchange, type, lot_size, face_value, cfi_code, cancellation, min_step, rating, margin_buy, margin_sell, margin_rate, price_step, price_max, price_min, theor_price, theor_price_limit, volatility, currency, isin, yield, primary_board, trading_status, trading_status_info, complex_product_category) " +
            "values(@Symbol, @Shortname, @Description, @Exchange, @Type, @LotSize, @FaceValue, @CfiCode, @Cancellation, @MinStep, @Rating, @MarginBuy, @MarginSell, @MarginRate, @PriceStep, @PriceMax, @PriceMin, @TheorPrice, @TheorPriceLimit, @Volatility, @Currency, @ISIN, @Yield, @PrimaryBoard, @TradingStatus, @TradingStatusInfo, @ComplexProductCategory) " +
            "on conflict (symbol) do update " +
            "set shortname = @Shortname, " +
            "    description = @Description, " +
            "    exchange = @Exchange, " +
            "    type = @Type, " +
            "    lot_size = @LotSize, " +
            "    face_value = @FaceValue, " +
            "    cfi_code = @CfiCode, " +
            "    min_step = @MinStep, " +
            "    rating = @Rating, " +
            "    margin_buy = @MarginBuy, " +
            "    margin_sell = @MarginSell, " +
            "    margin_rate = @MarginRate, " +
            "    price_step = @PriceStep, " +
            "    price_max = @PriceMax, " +
            "    price_min = @PriceMin, " +
            "    theor_price = @TheorPrice, " +
            "    theor_price_limit = @TheorPriceLimit, " +
            "    volatility = @Volatility, " +
            "    currency = @Currency, " +
            "    isin = @ISIN, " +
            "    yield = @Yield, " +
            "    primary_board = @PrimaryBoard, " +
            "    trading_status = @TradingStatus, " +
            "    trading_status_info = @TradingStatusInfo, " +
            "    complex_product_category = @ComplexProductCategory; ";

        await Connection.ExecuteAsync(query, instrument);
    }

    public void UpdateInstrument(Instrument instrument)
    {
        if (instrument == null) throw new ArgumentNullException(nameof(instrument));

        var query = "update instruments " +
                    "set price_min = @PriceMin, " +
                    "    price_max = @PriceMin, " +
                    "    margin_buy = @MarginBuy, " +
                    "    margin_sell = @MarginSell, " +
                    "    trading_status = @TradingStatus, " +
                    "    trading_status_info = @TradingStatusInfo, " +
                    "    theor_price = @TheorPrice, " +
                    "    theor_price_limit = @TheorPriceLimit, " +
                    "    volatility = @Volatility " +
                    "where symbol = @Symbol;";

        Connection.Execute(query, instrument);
    }
}