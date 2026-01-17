using System.Text.Json;
using NexusLedger.PaymentGateway.Infrastructure.Idempotency;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using StackExchange.Redis;

namespace NexusLedger.PaymentGateway.Tests.Infrastructure.Idempotency;

public class IdempotencyFilterTests
{
    private readonly Mock<IConnectionMultiplexer> _mockMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly IdempotencyFilter _filter;

    public IdempotencyFilterTests()
    {
        _mockMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        _filter = new IdempotencyFilter(_mockMultiplexer.Object);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldReturnBadRequest_WhenIdempotencyKeyIsMissing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var context = new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), 
            new List<IFilterMetadata>(), 
            new Dictionary<string, object?>(), 
            new Mock<Controller>().Object);

        var next = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, next.Object);

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Missing or empty X-Idempotency-Key header");
        
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldReturnCachedResult_WhenKeyExistsInRedis()
    {
        // Arrange
        var key = "test-key";
        var cachedValue = new { Status = "Processed" };
        var cachedRecord = new { StatusCode = 200, Value = cachedValue }; // Matching the record structure in Filter
        
        // We need to match the private record specific serialization if possible, 
        // but since it's private, we might rely on the fact that it's just JSON.
        // Actually, the record is private inside the class. 
        // Wait, looking at the code, `IdempotencyRecord` is private. 
        // I can simulate the JSON string that matches it.
        string json = JsonSerializer.Serialize(cachedRecord);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Idempotency-Key"] = key;

        var context = new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), 
            new List<IFilterMetadata>(), 
            new Dictionary<string, object?>(), 
            new Mock<Controller>().Object);

        _mockDatabase.Setup(db => db.StringGetAsync($"idempotency:{key}", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        var next = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, next.Object);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>();
        var result = context.Result as ObjectResult;
        result!.StatusCode.Should().Be(200);
        // We can't easily assert the Value equality directly effectively because of JsonElement vs Object but checking type/status is good for now
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldExecuteAndCache_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "new-key";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Idempotency-Key"] = key;

        var context = new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), 
            new List<IFilterMetadata>(), 
            new Dictionary<string, object?>(), 
            new Mock<Controller>().Object);

        // Mock Redis returning empty
        _mockDatabase.Setup(db => db.StringGetAsync($"idempotency:{key}", It.IsAny<CommandFlags>()))
             .ReturnsAsync(RedisValue.Null);

        // Setup next delegate to return a success result
        var executedContext = new ActionExecutedContext(
            context, new List<IFilterMetadata>(), new Mock<Controller>().Object);
        executedContext.Result = new OkObjectResult(new { Status = "Success" });

        ActionExecutionDelegate next = () => Task.FromResult(executedContext);

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        _mockDatabase.Verify(db => db.StringSetAsync(
            $"idempotency:{key}", 
            It.Is<RedisValue>(v => v.ToString().Contains("Success")), 
            It.IsAny<TimeSpan?>(), 
            false, 
            When.Always, 
            CommandFlags.None), Times.Once);
    }
}
