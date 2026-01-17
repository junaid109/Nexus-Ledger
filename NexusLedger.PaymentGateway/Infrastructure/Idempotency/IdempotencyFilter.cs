using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StackExchange.Redis;

namespace NexusLedger.PaymentGateway.Infrastructure.Idempotency;

public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IConnectionMultiplexer _redis;
    private const string HeaderName = "X-Idempotency-Key";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public IdempotencyFilter(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var idempKey) || string.IsNullOrWhiteSpace(idempKey))
        {
            context.Result = new BadRequestObjectResult($"Missing or empty {HeaderName} header");
            return;
        }

        var db = _redis.GetDatabase();
        var cacheKey = $"idempotency:{idempKey}";
        
        var cachedResponse = await db.StringGetAsync(cacheKey);
        if (!cachedResponse.IsNullOrEmpty)
        {
            var responseDto = JsonSerializer.Deserialize<IdempotencyRecord>(cachedResponse!);
            if (responseDto != null)
            {
                context.Result = new ObjectResult(responseDto.Value) { StatusCode = responseDto.StatusCode };
                return;
            }
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult && IsSuccessStatusCode(objectResult.StatusCode))
        {
            var record = new IdempotencyRecord(objectResult.StatusCode ?? 200, objectResult.Value);
            var serialized = JsonSerializer.Serialize(record);
            await db.StringSetAsync(cacheKey, serialized, CacheDuration);
        }
        else if (executedContext.Result is StatusCodeResult statusCodeResult && IsSuccessStatusCode(statusCodeResult.StatusCode))
        {
             var record = new IdempotencyRecord(statusCodeResult.StatusCode, null);
             var serialized = JsonSerializer.Serialize(record);
             await db.StringSetAsync(cacheKey, serialized, CacheDuration);
        }
    }

    private bool IsSuccessStatusCode(int? statusCode)
    {
        return statusCode.HasValue && statusCode.Value >= 200 && statusCode.Value < 300;
    }

    private record IdempotencyRecord(int StatusCode, object? Value);
}
