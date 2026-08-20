using System.Reflection;

namespace Mes.Domain.UnitTests.Architecture;

public sealed class DependencyRuleTests
{
    private static readonly string[] AllowedPrefixes =
    [
        "System",
        "netstandard",
        "mscorlib",
        "Microsoft.CSharp",
        "Microsoft.VisualBasic"
    ];

    [Fact]
    public void Domain_has_no_third_party_dependencies()
    {
        var domain = typeof(DomainAssemblyMarker).Assembly;

        var forbidden = domain
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => !AllowedPrefixes.Any(p =>
                name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Domain_does_not_reference_any_other_Mes_project()
    {
        var domain = typeof(DomainAssemblyMarker).Assembly;

        var mesReferences = domain
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith("Mes.", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(mesReferences);
    }
}
