using System.Threading.Tasks;
using Refit;

namespace Bablomet.Marketdata.External;

public interface IAlorOauthClient
{
    [Post("/refresh")]
    Task<RefreshTokenResponseDto> RefreshToken([Query] string token);
}