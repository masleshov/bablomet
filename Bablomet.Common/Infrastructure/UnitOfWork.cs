using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bablomet.Common.Repository;
using Npgsql;

namespace Bablomet.Common.Infrastructure;

public sealed class UnitOfWork : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly Dictionary<Type, BaseRepository> _repositories;

    public UnitOfWork()
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = EnvironmentGetter.GetVariable(EnvironmentVariables.POSTGRES_HOST),
            Port = EnvironmentGetter.GetIntVariable(EnvironmentVariables.POSTGRES_PORT),
            Username = EnvironmentGetter.GetVariable(EnvironmentVariables.POSTGRES_USER),
            Password = EnvironmentGetter.GetVariable(EnvironmentVariables.POSTGRES_PASSWORD),
            Database = EnvironmentGetter.GetVariable(EnvironmentVariables.POSTGRES_DATABASE),
            ApplicationName = EnvironmentGetter.GetVariable(EnvironmentVariables.POSTGRES_APP_NAME),
            MinPoolSize = EnvironmentGetter.GetIntVariable(EnvironmentVariables.POSTGRES_MIN_POOL_SIZE),
            MaxPoolSize = EnvironmentGetter.GetIntVariable(EnvironmentVariables.POSTGRES_MAX_POOL_SIZE),
            IncludeErrorDetail = EnvironmentGetter.GetBoolVariable(EnvironmentVariables.POSTGRES_INCLUDE_ERROR_DETAIL),
        }.ConnectionString;

        if(string.IsNullOrEmpty(connectionString))
        {
            throw new NullReferenceException("Connection string to database must be not null");
        }

        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        _repositories = GetRepositoriesByReflection();
    }

    /// <summary>
    /// Returns a repository by specified type
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Throws if repository hasn't been registered and can't be found</exception>
    public TRepository GetRepository<TRepository>() where TRepository : BaseRepository
    {
        if(!_repositories.TryGetValue(typeof(TRepository), out var repository))
        {
            throw new InvalidOperationException($"Repository of type {typeof(TRepository).Name} doesn't exist");
        }

        return (TRepository)repository;
    }

    private Dictionary<Type, BaseRepository> GetRepositoriesByReflection()
    {
        return Assembly.GetEntryAssembly().DefinedTypes
            .Where(type => type.IsSubclassOf(typeof(BaseRepository)))
            .ToDictionary(key => key.AsType(), value => (BaseRepository)Activator.CreateInstance(value.AsType(), _connection));
    }

    public void Dispose()
    {
        _repositories.Clear();
        _connection.Dispose();
    }
}