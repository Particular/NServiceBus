namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using Particular.AnalyzerTesting;

public class HandlerRegistryContributorAttributeAnalyzerTests : AnalyzerTestFixture<HandlerRegistryContributorAttributeAnalyzer>
{
    [Test]
    public Task DoesNotReportValidContributors()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryContributor]
                     public abstract class PublicContributor : IHandlerRegistryContributor
                     {
                         public static void AddHandlers(EndpointConfiguration configuration) { }
                         public static void AddSagas(EndpointConfiguration configuration) { }
                     }

                     [HandlerRegistryContributor]
                     internal sealed class InternalContributor : IHandlerRegistryContributor
                     {
                         public static void AddHandlers(EndpointConfiguration configuration) { }
                         public static void AddSagas(EndpointConfiguration configuration) { }
                     }

                     internal static class Holder
                     {
                         [HandlerRegistryContributor]
                         internal abstract class NestedContributor : IHandlerRegistryContributor
                         {
                             public static void AddHandlers(EndpointConfiguration configuration) { }
                             public static void AddSagas(EndpointConfiguration configuration) { }
                         }
                     }

                     public class NotMarked { }
                     """;

        return Assert(source);
    }

    [Test]
    public Task ReportsTypeThatDoesNotImplementTheInterface()
    {
        var source = """
                     using NServiceBus;

                     [[|HandlerRegistryContributor|]]
                     public class NotAContributor
                     {
                         public static void AddHandlers(EndpointConfiguration configuration) { }
                         public static void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public Task ReportsStaticClass()
    {
        var source = """
                     using NServiceBus;

                     [[|HandlerRegistryContributor|]]
                     public static class StaticContributor
                     {
                         public static void AddHandlers(EndpointConfiguration configuration) { }
                         public static void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public Task ReportsGenericContributor()
    {
        var source = """
                     using NServiceBus;

                     [[|HandlerRegistryContributor|]]
                     public abstract class GenericContributor<T> : IHandlerRegistryContributor
                     {
                         public static void AddHandlers(EndpointConfiguration configuration) { }
                         public static void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public Task ReportsContributorNestedInGenericType()
    {
        var source = """
                     using NServiceBus;

                     public static class Holder<T>
                     {
                         [[|HandlerRegistryContributor|]]
                         public abstract class NestedContributor : IHandlerRegistryContributor
                         {
                             public static void AddHandlers(EndpointConfiguration configuration) { }
                             public static void AddSagas(EndpointConfiguration configuration) { }
                         }
                     }
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public Task ReportsContributorThatIsNotAccessible()
    {
        var source = """
                     using NServiceBus;

                     public static class Holder
                     {
                         [[|HandlerRegistryContributor|]]
                         abstract class PrivateContributor : IHandlerRegistryContributor
                         {
                             public static void AddHandlers(EndpointConfiguration configuration) { }
                             public static void AddSagas(EndpointConfiguration configuration) { }
                         }
                     }
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public Task ReportsExplicitInterfaceImplementation()
    {
        var source = """
                     using NServiceBus;

                     [[|HandlerRegistryContributor|]]
                     public abstract class ExplicitContributor : IHandlerRegistryContributor
                     {
                         static void IHandlerRegistryContributor.AddHandlers(EndpointConfiguration configuration) { }
                         static void IHandlerRegistryContributor.AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public Task ReportsContributorInGeneratedCode()
    {
        var source = """
                     // <auto-generated/>
                     using NServiceBus;

                     [[|HandlerRegistryContributor|]]
                     public class NotAContributor { }
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public async Task ReportsAllProblemsInSingleDiagnostic()
    {
        var source = """
                     using NServiceBus;

                     public static class Holder
                     {
                         [HandlerRegistryContributor]
                         abstract class Contributor<T>
                         {
                         }
                     }
                     """;

        var output = GeneratedCompilation.Run(nameof(ReportsAllProblemsInSingleDiagnostic), source, []);
        var diagnostics = await output.Compilation
            .WithAnalyzers([new HandlerRegistryContributorAttributeAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

        var diagnostic = diagnostics.Single();
        NUnit.Framework.Assert.That(diagnostic.Id, Is.EqualTo(DiagnosticIds.InvalidHandlerRegistryContributor));
        NUnit.Framework.Assert.That(diagnostic.GetMessage(), Is.EqualTo("The handler registry contributor Contributor is not valid and is ignored: must not be generic, must be accessible from the generated registry, must implement IHandlerRegistryContributor."));
    }
}
