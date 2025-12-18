namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using Helpers;
using NUnit.Framework;

public class AddMultipleHandlersTests
{
    [Test]
    public void AddByAssembly()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     
                     namespace Particular.MyOutputAssembly.Some.Deep.Custom.NamespaceName;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandlersFromNamespace("Particular.MyOutputAssembly.Some.Deep.Custom.NamespaceName");
                         }
                     }
                     
                     public class HandleOrderCreated : IHandleMessages<OrderCreated>
                     {
                        public Task Handle(OrderCreated message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     
                     public class HandleOrderShipped: IHandleMessages<OrderShipped>
                     {
                        public Task Handle(OrderShipped message, IMessageHandlerContext context) => Task.CompletedTask;
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

        SourceGeneratorTest.ForIncrementalGenerator<AddMultipleHandlersInterceptor>("Particular.MyOutputAssembly")
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .ToConsole()
            .OutputSteps()
            .AssertRunsAreEqual();
    }
}