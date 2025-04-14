using System;
using System.Collections.Generic;

namespace Bablomet.Common.Infrastructure;

public static class EnvironmentGetter
{
    private static readonly Dictionary<EnvironmentVariables, string> _defaultValues = new()
    {
        { EnvironmentVariables.POSTGRES_HOST, "database" },
        { EnvironmentVariables.POSTGRES_PORT, "5432" },
        { EnvironmentVariables.POSTGRES_USER, "postgres" },
        { EnvironmentVariables.POSTGRES_PASSWORD, "6pA86~&|0Jzq" },
        { EnvironmentVariables.POSTGRES_DATABASE, "postgres" },
        { EnvironmentVariables.POSTGRES_APP_NAME, "Bablomet.Marketdata" },
        { EnvironmentVariables.POSTGRES_MIN_POOL_SIZE, "10" },
        { EnvironmentVariables.POSTGRES_MAX_POOL_SIZE, "50" },
        { EnvironmentVariables.POSTGRES_INCLUDE_ERROR_DETAIL, "false" },
        { EnvironmentVariables.KAFKA_HOST, "localhost" },
        { EnvironmentVariables.KAFKA_PORT, "9092" },
        { EnvironmentVariables.KAFKA_BARS_TOPIC, "bars" },
        { EnvironmentVariables.KAFKA_INDICATORS_TOPIC, "indicators" },
        { EnvironmentVariables.HOST, Guid.NewGuid().ToString() },
        { EnvironmentVariables.BACKTEST_FROM, "" },
        { EnvironmentVariables.BABLOMET_API_URI, "http://api:5001" },
        { EnvironmentVariables.BABLOMET_MARKETDATA_SAVE_BARS_TO_DB, "false" },
        { EnvironmentVariables.BABLOMET_PRO_TELEGRAM_TOKEN, "7645844434:AAGrThQixH6i3CZ7sWZIVpf-L6mdL_vU07U" },
    };

    public static int GetIntVariable(EnvironmentVariables variable)
    {
        return int.TryParse(GetVariable(variable), out int value) ?
            value :
            throw new NullReferenceException($"Unable to find {variable} variable's value either in environment or in dictionary");
    }

    public static bool GetBoolVariable(EnvironmentVariables variable)
    {
        return bool.TryParse(GetVariable(variable), out bool value) ?
            value :
            throw new NullReferenceException($"Unable to find {variable} variable's value either in environment or in dictionary");
    }

    public static string GetVariable(EnvironmentVariables variable)
    {
        if (variable == EnvironmentVariables.NONE)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        return Environment.GetEnvironmentVariable(variable.ToString())
            ?? _defaultValues.GetValueOrDefault(variable)
                ?? throw new NullReferenceException($"Unable to find {variable} variable's value either in environment or in dictionary");
    }
}

public enum EnvironmentVariables
{
    NONE = 0,
    POSTGRES_HOST = 1,
    POSTGRES_PORT = 2,
    POSTGRES_USER= 3,
    POSTGRES_PASSWORD = 4,
    POSTGRES_DATABASE = 5,
    POSTGRES_APP_NAME = 6,
    POSTGRES_MIN_POOL_SIZE = 7,
    POSTGRES_MAX_POOL_SIZE = 8,
    POSTGRES_INCLUDE_ERROR_DETAIL = 9,
    KAFKA_HOST = 10,
    KAFKA_PORT = 11,
    KAFKA_BARS_TOPIC = 12,
    KAFKA_INDICATORS_TOPIC = 121,
    KAFKA_BARS_AI_TRAINING_TOPIC = 1211,
    KAFKA_INDICATORS_AI_TRAINING_TOPIC = 1212,
    HOST = 13,
    BACKTEST_FROM = 14,

    BABLOMET_API_URI = 20,
    BABLOMET_MARKETDATA_SAVE_BARS_TO_DB = 30,

    BABLOMET_PRO_TELEGRAM_TOKEN = 100,
}
