using System.Reflection;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>ADR 0001 dependency rule, verified on every build: dependencies point toward the domain.</summary>
public class ArchitectureTests
{
    private static string[] References(Type anyTypeOfAssembly) =>
        anyTypeOfAssembly.Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();

    [Fact(DisplayName = "Domain depends on no other project, no XML, HTTP or crypto library")]
    public void Domain_depends_on_nothing()
    {
        var refs = References(typeof(Domain.ElectronicDocument));
        Assert.DoesNotContain(refs, r => r.StartsWith("Abc.", StringComparison.Ordinal));
        Assert.DoesNotContain(refs, r => r is "System.Xml" or "System.Private.Xml" or "System.Net.Http" or "System.Security.Cryptography.Xml");
    }

    [Fact(DisplayName = "Application depends only on Domain (never on Infrastructure or the UBL classes)")]
    public void Application_depends_only_on_domain()
    {
        var refs = References(typeof(Application.IssueDocument)).Where(r => r.StartsWith("Abc.", StringComparison.Ordinal));
        Assert.Equal(["Abc.Invoicing.Domain"], refs);
    }

    [Fact(DisplayName = "Only Infrastructure references the generated UBL classes (Abc.Ubl)")]
    public void Only_infrastructure_uses_the_generated_classes()
    {
        Assert.Contains("Abc.Ubl", References(typeof(Infrastructure.Ubl.Ubl21InvoiceGenerator)));
        Assert.DoesNotContain("Abc.Ubl", References(typeof(Application.IssueDocument)));
        Assert.DoesNotContain("Abc.Ubl", References(typeof(Domain.ElectronicDocument)));
    }

    [Fact(DisplayName = "The generated classes carry the XSD namespaces (they are not hand-written)")]
    public void Generated_classes_come_from_the_xsd()
    {
        var root = typeof(Abc.Ubl.Invoice.InvoiceType).GetCustomAttribute<System.Xml.Serialization.XmlRootAttribute>()!;
        Assert.Equal("Invoice", root.ElementName);
        Assert.Equal("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", root.Namespace);
        Assert.NotNull(typeof(Abc.Ubl.Invoice.InvoiceType).GetCustomAttribute<System.CodeDom.Compiler.GeneratedCodeAttribute>());
    }
}
