using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Refit;

namespace Bablomet.Common.Infrastructure;

public static class RefitConfigurationExtension
{
    private static readonly RefitSettings _settings;

    static RefitConfigurationExtension()
    {
        _settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            })
        };
    }

    public static IHttpClientBuilder AddRefitClient<TClient>(this IServiceCollection services, Uri serviceUri)
        where TClient : class
    {
        var advancedLogging = bool.TryParse(Environment.GetEnvironmentVariable("ADVANCED_LOGGING"), out var advancedLoggingVal) 
            ? advancedLoggingVal 
            : false;

        if (!advancedLogging)
        {
            services.AddScoped(sp => new LoggingHttpMessageHandler(sp.GetRequiredService<ILogger<LoggingHttpMessageHandler>>()));
        }
        else
        {
            services.AddScoped(sp => new HttpLoggingHandler(sp.GetRequiredService<ILogger<HttpLoggingHandler>>()));
        }

        var builder = services.AddRefitClient<TClient>(_settings)
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = serviceUri;
            });

        if (advancedLogging)
        {
            return builder.AddHttpMessageHandler<HttpLoggingHandler>();
        }
        
        return builder.AddHttpMessageHandler<LoggingHttpMessageHandler>();
    }
}
