namespace NServiceBus.Core.Analyzer.Tests.Sagas;

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
                             mapper.ConfigureMapping<OrderCreated>(m => m.OrderId).ToSaga(s => s.OrderId);
                             mapper.ConfigureMapping<OrderShipped>(m => m.OrderId).ToSaga(s => s.OrderId);
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
            .WithGeneratorStages("InterceptCandidates", "Collected")
            .Approve()
            .ToConsole()
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
                             mapper.ConfigureMapping<OrderCreated>(m => m.OrderId).ToSaga(s => s.OrderId);
                         }
                         
                         public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class PaymentSaga : Saga<PaymentSagaData>,
                         IAmStartedByMessages<PaymentRequested>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PaymentSagaData> mapper)
                         {
                             mapper.ConfigureMapping<PaymentRequested>(m => m.PaymentId).ToSaga(s => s.PaymentId);
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
            .WithGeneratorStages("InterceptCandidates", "Collected")
            .Approve()
            .ToConsole()
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
                             mapper.ConfigureHeaderMapping<OrderCreated>("NServiceBus.CorrelationId").ToSaga(s => s.OrderId);
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
            .WithGeneratorStages("InterceptCandidates", "Collected")
            .Approve()
            .ToConsole()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithMapSagaSyntax()
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
                             mapper.MapSaga(saga => saga.OrderId)
                                 .ToMessage<OrderCreated>(msg => msg.OrderId)
                                 .ToMessage<OrderShipped>(msg => msg.OrderId);
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
            .WithGeneratorStages("InterceptCandidates", "Collected")
            .Approve()
            .ToConsole()
            .AssertRunsAreEqual();
    }
}

