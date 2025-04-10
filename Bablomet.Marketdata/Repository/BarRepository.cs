using System;
using Bablomet.Common.Domain;
using Bablomet.Common.Repository;
using Dapper;
using Npgsql;

namespace Bablomet.Marketdata.Repository;

public sealed class BarRepository : BaseRepository
{
    public BarRepository(NpgsqlConnection connection) : base(connection)
    {
    }
    
    public int AddBarIfNotExists(Bar bar)
    {
        if (bar == null) throw new ArgumentNullException(nameof(bar));
        if (bar.BarId > 0) throw new ArgumentException("Bar is already initialized");

        var query =
            "insert into bars (symbol, time_frame, time, close, open, high, low, volume) " +
            "select @Symbol, @TimeFrame, @Time, @Close, @Open, @High, @Low, @Volume " +
            "returning bar_id; ";

        return Connection.QuerySingleOrDefault<int>(query, bar);
    }
}