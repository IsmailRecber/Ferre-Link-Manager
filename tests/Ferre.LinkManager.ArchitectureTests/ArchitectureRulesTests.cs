using System.Reflection;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Domain.Common;
using NetArchTest.Rules;
using Xunit;

namespace Ferre.LinkManager.ArchitectureTests;

public class ArchitectureRulesTests
{
    private const string DomainNs = "Ferre.LinkManager.Domain";
    private const string ApplicationNs = "Ferre.LinkManager.Application";
    private const string InfrastructureNs = "Ferre.LinkManager.Infrastructure";
    private const string ApiNs = "Ferre.LinkManager.Api";

    private static readonly Assembly Domain = typeof(Entity).Assembly;
    private static readonly Assembly Application = typeof(IUnitOfWork).Assembly;
    private static readonly Assembly Infrastructure = typeof(Ferre.LinkManager.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnOtherLayers()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Domain_DoesNotDependOnFrameworks()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNs, ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnAspNetCore()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Handlers_AreSealedAndNamedWithHandlerSuffix()
    {
        var sealedResult = Types.InAssembly(Application)
            .That()
            .ResideInNamespaceContaining("Ferre.LinkManager.Application")
            .And()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(sealedResult.IsSuccessful, Describe(sealedResult));
    }

    [Fact]
    public void AggregateRoots_HaveNoPublicConstructors()
    {
        var aggregateRoots = Domain.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(AggregateRoot).IsAssignableFrom(type))
            .ToList();

        Assert.NotEmpty(aggregateRoots);

        var offenders = aggregateRoots
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, "Public constructors found on: " + string.Join(", ", offenders));
    }

    private static string Describe(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Violating types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}