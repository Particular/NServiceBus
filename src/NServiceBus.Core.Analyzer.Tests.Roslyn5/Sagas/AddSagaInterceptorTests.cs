namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using Analyzer.Handlers;
using Analyzer.Sagas;
using Helpers;
using NUnit.Framework;

public class AddSagaInterceptorTests
{
    [Test]
    public void BasicSaga()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleMessages<OrderShipped>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId)
                                 .ToMessage<OrderCreated>(m => m.OrderId)
                                 .ToMessage<OrderShipped>(m => m.OrderId);
                         }
                         
                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderShipped : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void AttributeOnClass()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     
                     [NServiceBusRegistrations]
                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleMessages<OrderShipped>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId)
                                 .ToMessage<OrderCreated>(m => m.OrderId)
                                 .ToMessage<OrderShipped>(m => m.OrderId);
                         }
                         
                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderShipped : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NoAttributeNoOutput()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleMessages<OrderShipped>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId)
                                 .ToMessage<OrderCreated>(m => m.OrderId)
                                 .ToMessage<OrderShipped>(m => m.OrderId);
                         }
                         
                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderShipped : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .ShouldNotGenerateCode()
            .AssertRunsAreEqual();
    }

    [Test]
    public void DisableUnsafeAccessors()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleMessages<OrderShipped>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId)
                                 .ToMessage<OrderCreated>(m => m.OrderId)
                                 .ToMessage<OrderShipped>(m => m.OrderId);
                         }
                         
                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderShipped : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .WithProperty("build_property.NServiceBusDisableSagaPropertyAccessor", "true")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void MultipleSagas()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                             cfg.AddSaga<PaymentSaga>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId).ToMessage<OrderCreated>(m => m.OrderId);
                         }
                         
                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class PaymentSaga : Saga<PaymentSagaData>,
                         IAmStartedByMessages<PaymentRequested>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PaymentSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.PaymentId).ToMessage<PaymentRequested>(m => m.PaymentId);
                         }
                         
                         public Task Handle(PaymentRequested message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class PaymentSagaData : ContainSagaData
                     {
                         public string PaymentId { get; set; }
                     }
                     
                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class PaymentRequested : IEvent
                     {
                         public string PaymentId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithHeaderMapping()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId).ToMessageHeader<OrderCreated>("NServiceBus.CorrelationId");
                         }
                         
                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     
                     public class OrderCreated : IEvent
                     {
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithMixedPropertyAndHeaderMappings()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleMessages<OrderShipped>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId)
                                 .ToMessage<OrderCreated>(m => m.OrderId)
                                 .ToMessageHeader<OrderShipped>("NServiceBus.CorrelationId");
                         }

                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderShipped : IEvent
                     {
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithTimeoutHandling()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleTimeouts<OrderTimeout>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId).ToMessage<OrderCreated>(m => m.OrderId);
                         }

                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Timeout(OrderTimeout state, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderTimeout
                     {
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithMultipleMessageTypes()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleMessages<OrderShipped>,
                         IHandleMessages<OrderCancelled>,
                         IHandleTimeouts<OrderTimeout>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             mapper.MapSaga(s => s.OrderId)
                                 .ToMessage<OrderCreated>(m => m.OrderId)
                                 .ToMessage<OrderShipped>(m => m.OrderId)
                                 .ToMessage<OrderCancelled>(m => m.OrderId);
                         }

                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderCancelled message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Timeout(OrderTimeout state, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderShipped : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderCancelled : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderTimeout
                     {
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithMultipleMessagesAndNonFluentSyntax()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderSaga>();
                         }
                     }

                     public class OrderSaga : Saga<OrderSagaData>,
                         IAmStartedByMessages<OrderCreated>,
                         IHandleMessages<OrderShipped>,
                         IHandleMessages<OrderCancelled>,
                         IHandleTimeouts<OrderTimeout>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                         {
                             var innerMapper = mapper.MapSaga(s => s.OrderId);
                             innerMapper.ToMessage<OrderCreated>(m => m.OrderId);
                             innerMapper.ToMessage<OrderShipped>(m => m.OrderId);
                             innerMapper.ToMessage<OrderCancelled>(m => m.OrderId);
                         }

                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderCancelled message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Timeout(OrderTimeout state, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderCreated : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderShipped : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderCancelled : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderTimeout
                     {
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithInappropriateDoubleMessageMapping()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderPolicy>();
                         }
                     }

                     public class OrderPolicy : Saga<OrderPolicyData>,
                         IAmStartedByMessages<OrderPlaced>,
                         IAmStartedByMessages<OrderBilled>,
                         IHandleTimeouts<OrderPlaced> // Should not also use a message as timeout state in real life!
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                 .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                 .ToMessage<OrderBilled>(msg => msg.OrderId);
                         }
                         
                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderPolicyData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     public class OrderPlaced : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     public class OrderBilled : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<AddHandlerOnSagaTypeAnalyzer>()
            .WithGeneratorStages("SagaSpec", "SagaSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }
}
