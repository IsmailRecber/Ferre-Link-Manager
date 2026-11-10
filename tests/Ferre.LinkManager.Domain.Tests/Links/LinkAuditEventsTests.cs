using Ferre.LinkManager.Domain.Links;
using Xunit;

namespace Ferre.LinkManager.Domain.Tests.Links;

public class LinkAuditEventsTests
{
    private static Link CreateLink()
    {
        var link = Link.Create("Payroll", "https://payroll.local", Guid.NewGuid(), new[] { "User" });

        // Forget the "created" event, so each test sees only the events it caused.
        link.ClearDomainEvents();

        return link;
    }

    [Fact]
    public void Deactivate_ActiveLink_RaisesDeactivatedEventWithTitleAndUrl()
    {
        var link = CreateLink();

        link.Deactivate();

        var domainEvent = Assert.IsType<LinkDeactivated>(Assert.Single(link.DomainEvents));
        Assert.Equal(link.Id, domainEvent.LinkId);
        Assert.Equal("Payroll", domainEvent.Title);
        Assert.Equal("https://payroll.local", domainEvent.Url);
    }

    [Fact]
    public void Deactivate_Twice_RaisesOnlyOneEvent()
    {
        var link = CreateLink();

        link.Deactivate();
        link.Deactivate();

        Assert.Single(link.DomainEvents);
    }

    [Fact]
    public void Activate_AfterDeactivate_RaisesActivatedEvent()
    {
        var link = CreateLink();
        link.Deactivate();
        link.ClearDomainEvents();

        link.Activate();

        Assert.IsType<LinkActivated>(Assert.Single(link.DomainEvents));
    }

    [Fact]
    public void Activate_WhenAlreadyActive_RaisesNothing()
    {
        var link = CreateLink();

        link.Activate();

        Assert.Empty(link.DomainEvents);
    }
}