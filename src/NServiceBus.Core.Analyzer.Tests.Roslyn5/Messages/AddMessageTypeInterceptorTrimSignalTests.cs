namespace NServiceBus.Core.Analyzer.Tests.Messages;

using Analyzer.Messages;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class AddMessageTypeInterceptorTrimSignalTests
{
    [Test]
    public void EnableTrimAnalyzer_generates_interceptors_but_not_the_runtime_trimming_signal()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddMessageType<MyMessage>();
                         }
                     }

                     public class MyMessage : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        var withTrimAnalyzer = SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .WithProperty("build_property.EnableTrimAnalyzer", "true")
            .Run()
            .GetCompilationOutput();
        using (Assert.EnterMultipleScope())
        {
            // Interceptor support must be generated in warning-only (EnableTrimAnalyzer) builds so the
            // reflection fallback warning can be suppressed there.
            Assert.That(withTrimAnalyzer, Does.Contain("InterceptionsOfAddMessageTypeMethod.g.cs"));
            // EnableTrimAnalyzer alone must NOT emit the runtime strict-mode signal.
            Assert.That(withTrimAnalyzer, Does.Not.Contain("TrimmingEnabled.g.cs"));
        }

        var withPublishTrimmed = SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .WithProperty("build_property.PublishTrimmed", "true")
            .Run()
            .GetCompilationOutput();
        Assert.That(withPublishTrimmed, Does.Contain("TrimmingEnabled.g.cs"));

        var withoutAnyTrimProperty = SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .Run()
            .GetCompilationOutput();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutAnyTrimProperty, Does.Contain("InterceptionsOfAddMessageTypeMethod.g.cs"));
            Assert.That(withoutAnyTrimProperty, Does.Not.Contain("TrimmingEnabled.g.cs"));
        }
    }
}
