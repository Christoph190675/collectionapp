using CollectionApp.Application.Health;
using Microsoft.Extensions.Time.Testing;

namespace CollectionApp.Tests.Health;

public class GetHealthStatusHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 15, 42, TimeSpan.Zero);

    private sealed class StubChecker(bool healthy) : IDatabaseHealthChecker
    {
        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(healthy);
    }

    [Fact]
    public async Task HealthyDatabase_ReturnsOkOk_WithUtcCheckedAt()
    {
        var handler = new GetHealthStatusQueryHandler(new StubChecker(true), new FakeTimeProvider(Now));

        var result = await handler.Handle(new GetHealthStatusQuery(), CancellationToken.None);

        Assert.Equal("ok", result.Api);
        Assert.Equal("ok", result.Database);
        Assert.Equal(Now.UtcDateTime, result.CheckedAt);
        Assert.Equal(DateTimeKind.Utc, result.CheckedAt.Kind);
    }

    [Fact]
    public async Task UnhealthyDatabase_ReturnsApiOk_DatabaseKo()
    {
        var handler = new GetHealthStatusQueryHandler(new StubChecker(false), new FakeTimeProvider(Now));

        var result = await handler.Handle(new GetHealthStatusQuery(), CancellationToken.None);

        Assert.Equal("ok", result.Api);
        Assert.Equal("ko", result.Database);
    }
}
