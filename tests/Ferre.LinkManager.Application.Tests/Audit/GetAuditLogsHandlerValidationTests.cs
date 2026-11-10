using Ferre.LinkManager.Application.Audit.GetAuditLogs;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Tests.Fakes;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Audit;

public class GetAuditLogsHandlerValidationTests
{
    private static GetAuditLogsHandler CreateHandler() => new(new FakeReadDbContext(), new FakeIdentityService());

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidPage_Throws(int page)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new GetAuditLogsQuery(page, null, null, null, null), CancellationToken.None));

        Assert.Contains("InvalidPage", exception.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task InvalidPageSize_Throws(int pageSize)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new GetAuditLogsQuery(1, pageSize, null, null, null), CancellationToken.None));

        Assert.Contains("InvalidPageSize", exception.Errors);
    }

    [Fact]
    public async Task UnknownAction_Throws()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new GetAuditLogsQuery(1, 20, "Link.Exploded", null, null), CancellationToken.None));

        Assert.Contains("InvalidAction", exception.Errors);
    }

    [Fact]
    public async Task FromAfterTo_Throws()
    {
        var from = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().HandleAsync(new GetAuditLogsQuery(1, 20, null, from, to), CancellationToken.None));

        Assert.Contains("InvalidDateRange", exception.Errors);
    }
}