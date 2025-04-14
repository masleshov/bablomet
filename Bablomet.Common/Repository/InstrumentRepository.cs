using System.Linq;
using System.Threading.Tasks;
using Bablomet.Common.Domain;
using Bablomet.Common.Repository;
using Dapper;
using Npgsql;

namespace Bablomet.Common.Repository;

internal sealed class InstrumentRepository : BaseRepository
{
    public InstrumentRepository(NpgsqlConnection connection) : base(connection)
    {
    }
    
    public async Task<Instrument[]> GetInstruments()
    {
        var query = "select symbol as Symbol " +
                    "     , shortname as Shortname " +
                    "     , description as Description " + 
                    "     , exchange as Exchange " +
                    "     , type as Type " +
                    "     , lot_size as LotSize " +
                    "     , face_value as FaceValue " +
                    "     , cfi_code as CfiCode " +
                    "     , cancellation as Cancellation " + 
                    "     , min_step as MinStep " +
                    "     , rating as Rating " +
                    "     , margin_buy as MarginBuy " +
                    "     , margin_sell as MarginSell " +
                    "     , margin_rate as MarginRate " +
                    "     , price_step as PriceStep " +
                    "     , price_max as PriceMax " +
                    "     , price_min as PriceMin " +
                    "     , theor_price as TheorPrice " +
                    "     , theor_price_limit as TheorPriceLimit " + 
                    "     , volatility as Volatility " +
                    "     , currency as Currency " +
                    "     , isin as ISIN " + 
                    "     , yield as Yield " +
                    "     , primary_board as PrimaryBoard " + 
                    "     , trading_status as TradingStatus " +
                    "     , trading_status_info as TradingStatusInfo " +
                    "     , complex_product_category as ComplexProductCategory " +
                    "from instruments; ";
        return (await Connection.QueryAsync<Instrument>(query)).ToArray();
    }
}