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
                     public sealed class PublicContributor : IHandlerRegistryContributor<PublicContributor>
                     {
                         public static PublicContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }

                     [HandlerRegistryContributor]
                     internal sealed class InternalContributor : IHandlerRegistryContributor<InternalContributor>
                     {
                         public static InternalContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }

                     [HandlerRegistryContributor]
                     internal sealed class ExplicitContributor : IHandlerRegistryContributor<ExplicitContributor>
                     {
                         static ExplicitContributor IHandlerRegistryContributor<ExplicitContributor>.Instance { get; } = new();

                         void IHandlerRegistryContributor.AddHandlers(EndpointConfiguration configuration) { }
                         void IHandlerRegistryContributor.AddSagas(EndpointConfiguration configuration) { }
                     }

                     [HandlerRegistryContributor]
                     internal sealed class NonGenericContributor : IHandlerRegistryContributor
                     {
                         public static IHandlerRegistryContributor Instance { get; } = new NonGenericContributor();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }

                     [HandlerRegistryContributor]
                     internal sealed class ExplicitNonGenericContributor : IHandlerRegistryContributor
                     {
                         static IHandlerRegistryContributor IHandlerRegistryContributor.Instance { get; } = new ExplicitNonGenericContributor();

                         void IHandlerRegistryContributor.AddHandlers(EndpointConfiguration configuration) { }
                         void IHandlerRegistryContributor.AddSagas(EndpointConfiguration configuration) { }
                     }

                     internal static class Holder
                     {
                         [HandlerRegistryContributor]
                         internal sealed class NestedContributor : IHandlerRegistryContributor<NestedContributor>
                         {
                             public static NestedContributor Instance { get; } = new();

                             public void AddHandlers(EndpointConfiguration configuration) { }
                             public void AddSagas(EndpointConfiguration configuration) { }
                         }
                     }

                     public class NotMarked { }
                     """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportValidRecordContributor()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryContributor]
                     internal sealed record class RecordContributor : IHandlerRegistryContributor<RecordContributor>
                     {
                         public static RecordContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        return Assert(source);
    }

    [Test]
    public Task ReportsRecordClassThatDoesNotImplementTheInterface()
    {
        var source = """
                     using NServiceBus;

                     [[|HandlerRegistryContributor|]]
                     public sealed record class RecordContributor;
                     """;

        return Assert(source, DiagnosticIds.InvalidHandlerRegistryContributor);
    }

    [Test]
    public Task ReportsTypeThatDoesNotImplementTheInterface()
    {
        var source = """
                     using NServiceBus;

                     [[|HandlerRegistryContributor|]]
                     public class NotAContributor
                     {
                         public static NotAContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
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
                     public class GenericContributor<T> : IHandlerRegistryContributor<GenericContributor<T>>
                     {
                         public static GenericContributor<T> Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
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
                         public class NestedContributor : IHandlerRegistryContributor<NestedContributor>
                         {
                             public static NestedContributor Instance { get; } = new();

                             public void AddHandlers(EndpointConfiguration configuration) { }
                             public void AddSagas(EndpointConfiguration configuration) { }
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
                         class PrivateContributor : IHandlerRegistryContributor<PrivateContributor>
                         {
                             public static PrivateContributor Instance { get; } = new();

                             public void AddHandlers(EndpointConfiguration configuration) { }
                             public void AddSagas(EndpointConfiguration configuration) { }
                         }
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
                         class Contributor<T>
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
