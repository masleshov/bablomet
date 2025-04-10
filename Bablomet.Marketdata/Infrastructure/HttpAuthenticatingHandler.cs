using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bablomet.Marketdata.Infrastructure;

public class HttpAuthenticatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(AlorJwtHolder.Jwt))
        {
            request.Headers.Add("Authorization", $"Bearer {AlorJwtHolder.Jwt}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}