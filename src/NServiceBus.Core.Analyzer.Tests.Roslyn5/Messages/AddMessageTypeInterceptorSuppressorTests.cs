namespace NServiceBus.Core.Analyzer.Tests.Messages;

using Analyzer.Messages;
using Helpers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class AddMessageTypeInterceptorSuppressorTests
{
    [Test]
    public void SuppressesIL2026ForAddMessageType()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddMessageType<SampleMessage>();
                         }
                     }

                     public class SampleMessage : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<MockTrimmingAnalyzer>()
            .WithSuppressor<AddMessageTypeInterceptorSuppressor>()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Not.Contain("IL2026"));
    }

    [Test]
    public void DoesNotSuppressIL2026ForAddMessageTypeWithGenericTypeParameter()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             Register<MyMessage>(cfg);
                         }

                         // The type argument is a generic type parameter, so no interceptor can be generated
                         // and the RequiresUnreferencedCode fallback warning must not be suppressed.
                         public void Register<TMessage>(EndpointConfiguration cfg) where TMessage : IMessage
                         {
                             cfg.AddMessageType<TMessage>();
                         }
                     }

                     public class MyMessage : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<MockTrimmingAnalyzer>()
            .WithSuppressor<AddMessageTypeInterceptorSuppressor>()
            .SuppressDiagnosticErrors()
            .SuppressCompilationErrors()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Contain("IL2026"));
    }

    [Test]
    public void DoesNotSuppressIL2026ForNonAddMessageTypeCalls()
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

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<MockTrimmingAnalyzer>()
            .WithSuppressor<AddMessageTypeInterceptorSuppressor>()
            .SuppressDiagnosticErrors()
            .SuppressCompilationErrors()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Contain("IL2026"));
    }
}
