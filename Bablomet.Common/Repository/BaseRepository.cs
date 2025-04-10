using Npgsql;

namespace Bablomet.Common.Repository;

public abstract class BaseRepository
{
    protected readonly NpgsqlConnection Connection;

    public BaseRepository(NpgsqlConnection connection) => Connection = connection;
}