using Bablomet.API.Infrastructure;
using Bablomet.Common.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bablomet.API.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class IndicatorsController : ControllerBase
{
    private readonly ILogger<IndicatorsController> _logger;

    public IndicatorsController(ILogger<IndicatorsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Subscribe([FromBody] IndicatorSubscription subscription)
    {
        IndicatorSubscriptionsCache.Subscribe(subscription);
        _logger.LogInformation($"Subscribed on {subscription.IndicatorType} {subscription.Symbol} {subscription.TimeFrame}, parameters: {string.Join(",", subscription.Parameters)}");
        return Ok();
    }

    [HttpPost]
    public IActionResult Unsubscribe([FromBody] IndicatorSubscription subscription)
    {
        IndicatorSubscriptionsCache.Unsubscribe(subscription);
        _logger.LogInformation($"Unsubscribed from {subscription.IndicatorType} {subscription.Symbol} {subscription.TimeFrame}, parameters: {string.Join(",", subscription.Parameters)}");
        return Ok();
    }
}