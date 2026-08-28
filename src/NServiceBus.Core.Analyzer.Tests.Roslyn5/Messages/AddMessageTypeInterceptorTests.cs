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
}
