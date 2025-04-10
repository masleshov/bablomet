using System;
using System.Threading;
using System.Threading.Tasks;
using Bablomet.Marketdata.External;

namespace Bablomet.Marketdata.Infrastructure;

public static class AlorJwtHolder
{
    private static readonly TimeSpan SyncRootTimeout = TimeSpan.FromSeconds(30);

    private static Timer _timer;
    private static readonly ReaderWriterLock _syncRoot = new();

    private static string _jwt;
    public static string Jwt
    {
        get
        {
            _syncRoot.AcquireReaderLock(SyncRootTimeout);
            try
            {
                return _jwt;
            }
            finally
            {
                _syncRoot.ReleaseReaderLock();
            }
        }
        set
        {
            _syncRoot.AcquireWriterLock(SyncRootTimeout);
            try
            {
                _jwt = value;
            }
            finally
            {
                _syncRoot.ReleaseWriterLock();
            }
        }
    }

    public static string RefreshToken { get; private set; }

    public static async Task Init(IAlorOauthClient alorClient)
    {
        RefreshToken = Environment.GetEnvironmentVariable("ALOR_REFRESH_TOKEN");
        Jwt = (await alorClient.RefreshToken(RefreshToken)).AccessToken;

        const int refreshPeriod = 10 * 60 * 1000;
        _timer = new Timer(async _ => 
        {
            Jwt = (await alorClient.RefreshToken(RefreshToken)).AccessToken;
        }, null, refreshPeriod, refreshPeriod);
    }

    public static void Dispose()
    {
        _timer.Dispose();
    }
}