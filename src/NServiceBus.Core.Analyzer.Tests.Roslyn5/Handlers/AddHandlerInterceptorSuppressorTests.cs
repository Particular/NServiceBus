namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class AddHandlerInterceptorSuppressorTests
{
    [Test]
    public void SuppressesIL2026ForAddHandler()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<SampleHandler>();
                         }
                     }

                     public class SampleHandler : IHandleMessages<SampleCommand>
                     {
                         public Task Handle(SampleCommand cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class SampleCommand : ICommand { }
                     """;

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<MockTrimmingAnalyzer>()
            .WithSuppressor<AddHandlerInterceptorSuppressor>()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Not.Contain("IL2026"));
    }

    [Test]
    public void DoesNotSuppressIL2026ForNonAddHandlerCalls()
    {
        var source = """
                     using System.Diagnostics.CodeAnalysis;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             // This call should still produce IL2026 since it's not intercepted
                             SomeOtherMethod();
                         }

                         [RequiresUnreferencedCode("Test method")]
                         public void SomeOtherMethod() { }
                     }
                     """;

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<MockTrimmingAnalyzer>()
            .WithSuppressor<AddHandlerInterceptorSuppressor>()
            .SuppressDiagnosticErrors()
            .SuppressCompilationErrors()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Contain("IL2026"));
    }
}