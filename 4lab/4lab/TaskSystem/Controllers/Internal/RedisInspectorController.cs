using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace TaskSystem.Controllers.Internal;

[ApiController]
[Route("internal/redis")]
[ApiExplorerSettings(IgnoreApi = true)]
public class RedisInspectorController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;

    public RedisInspectorController(
        IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        var endpoint = _redis.GetEndPoints().First();

        var server = _redis.GetServer(endpoint);

        return Ok(new
        {
            info = server.Info(),
            keys = server.DatabaseSize()
        });
    }
}
