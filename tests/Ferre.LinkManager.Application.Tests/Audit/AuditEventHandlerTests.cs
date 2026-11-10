using Ferre.LinkManager.Application.Audit;
using Ferre.LinkManager.Application.Tests.Fakes;
using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Application.Tests.Audit;

public class AuditEventHandlerTests
{
    private readonly FakeAuditLogRepository _logs = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _adminId = Guid.NewGuid();

    private AuditEventHandler CreateHandler() => new(_logs, _unitOfWork, new FakeCurrentUser(_adminId));

    [Fact]
    public async Task LinkDeactivated_IsRecordedWithTitleUrlAndUser()
    {
        var linkId = Guid.NewGuid();

        await CreateHandler().HandleAsync(
            new LinkDeactivated(linkId, "Payroll", "https://payroll.local"), CancellationToken.None);

        var log = Assert.Single(_logs.Items);
        Assert.Equal(AuditActions.LinkDeactivated, log.Action);
        Assert.Equal("Link", log.EntityType);
        Assert.Equal(linkId, log.EntityId);
        Assert.Equal("Payroll", log.EntityName);
        Assert.Equal("https://payroll.local", log.EntityUrl);
        Assert.Equal(_adminId, log.UserId);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CategoryDeactivated_IsRecordedWithName()
    {
        var categoryId = Guid.NewGuid();

        await CreateHandler().HandleAsync(new CategoryDeactivated(categoryId, "Finance"), CancellationToken.None);

        var log = Assert.Single(_logs.Items);
        Assert.Equal(AuditActions.CategoryDeactivated, log.Action);
        Assert.Equal("Category", log.EntityType);
        Assert.Equal("Finance", log.EntityName);
        Assert.Null(log.EntityUrl);
    }

    [Fact]
    public async Task CategoryRenamed_KeepsBothTheOldAndTheNewName()
    {
        await CreateHandler().HandleAsync(
            new CategoryRenamed(Guid.NewGuid(), "Finance", "Accounting"), CancellationToken.None);

        var log = Assert.Single(_logs.Items);
        Assert.Equal(AuditActions.CategoryRenamed, log.Action);
        Assert.Equal("Finance → Accounting", log.EntityName);
    }

    [Fact]
    public async Task CategoryRenamed_WithTwoVeryLongNames_IsShortenedToFitTheColumn()
    {
        var oldName = new string('a', 100);
        var newName = new string('b', 100);

        await CreateHandler().HandleAsync(
            new CategoryRenamed(Guid.NewGuid(), oldName, newName), CancellationToken.None);

        var log = Assert.Single(_logs.Items);
        Assert.NotNull(log.EntityName);
        Assert.Equal(AuditLog.MaxEntityNameLength, log.EntityName!.Length);
        Assert.EndsWith("…", log.EntityName);
        Assert.StartsWith(oldName, log.EntityName);
    }

    [Fact]
    public async Task LinkActivated_IsRecordedWithItsOwnAction()
    {
        await CreateHandler().HandleAsync(
            new LinkActivated(Guid.NewGuid(), "Payroll", "https://payroll.local"), CancellationToken.None);

        Assert.Equal(AuditActions.LinkActivated, Assert.Single(_logs.Items).Action);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLog> Items { get; } = new();

        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
        {
            Items.Add(auditLog);
            return Task.CompletedTask;
        }
    }
}