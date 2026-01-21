namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using Helpers;
using NUnit.Framework;

public class AddHandlerInterceptorTests
{
    [Test]
    public void BasicHandlers()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : ICommand { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .ToConsole()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NestedHandler()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using Orders.Shipping;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OuterClass.OrderShippedHandler>();
                             cfg.AddHandler<AnotherOuterClass.InnerClass.OrderShippedHandler>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<AnotherOuterClass.InnerClass.OrderShippedHandler>();
                         }
                     }

                     namespace Orders.Shipping
                     {
                         public class OuterClass
                         {
                             [HandlerAttribute]
                             public class OrderShippedHandler : IHandleMessages<Cmd1>
                             {
                                 public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                             }
                         }
                         
                         public class AnotherOuterClass
                         {
                             public class InnerClass 
                             {
                                  [HandlerAttribute]
                                  public class OrderShippedHandler : IHandleMessages<Cmd1>
                                  {
                                      public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                                  }
                             }
                         }
                     }

                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
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
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderPolicy>();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .ToConsole()
            .AssertRunsAreEqual();
    }
}