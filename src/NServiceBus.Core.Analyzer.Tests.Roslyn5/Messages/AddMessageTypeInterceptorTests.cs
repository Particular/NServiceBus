namespace NServiceBus.Core.Analyzer.Tests.Messages;

using Analyzer.Messages;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class AddMessageTypeInterceptorTests
{
    [Test]
    public void BasicMessageTypes()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddMessageType<Messages.OrderPlaced>();
                             cfg.AddMessageType<Messages.OrderBilled>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddMessageType<Messages.OrderBilled>();
                         }
                     }

                     namespace Messages
                     {
                         public class OrderPlaced : IEvent
                         {
                             public string OrderId { get; set; }
                         }

                         public class OrderBilled : IEvent
                         {
                             public string OrderId { get; set; }
                         }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void MessageTypesWithEqualRankInterfaces()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddMessageType<Messages.OrderAccepted>();
                         }
                     }

                     namespace Messages
                     {
                         // Declared in reverse-alphabetical order: equal-rank interfaces must keep declaration order
                         // (matching runtime reflection and handler generation) rather than being alphabetically reordered.
                         public class OrderAccepted : OrderEventBase, ISecond, IFirst
                         {
                         }

                         public class OrderEventBase : IEvent
                         {
                             public string OrderId { get; set; }
                         }

                         public interface IFirst : IEvent
                         {
                         }

                         public interface ISecond : IEvent
                         {
                         }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void MessageTypesWithHierarchy()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddMessageType<Messages.OrderAccepted>();
                             cfg.AddMessageType<Messages.OrderRejected>();
                         }
                     }

                     namespace Messages
                     {
                         public class OrderAccepted : OrderEventBase, IOrderEvent
                         {
                         }

                         public class OrderRejected : OrderEventBase
                         {
                         }

                         public class OrderEventBase : IEvent
                         {
                             public string OrderId { get; set; }
                         }

                         public interface IOrderEvent : IEvent
                         {
                         }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void Interceptors_are_generated_in_warning_only_trim_analyzer_builds()
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

        // Interceptor support must be generated in warning-only (EnableTrimAnalyzer) builds so the reflection
        // fallback warning can be suppressed there.
        var output = SourceGeneratorTest.ForIncrementalGenerator<AddMessageTypeInterceptor>()
            .WithSource(source, "test.cs")
            .WithProperty("build_property.EnableTrimAnalyzer", "true")
            .Run()
            .GetCompilationOutput();

        Assert.That(output, Does.Contain("InterceptionsOfAddMessageTypeMethod.g.cs"));
    }
}
