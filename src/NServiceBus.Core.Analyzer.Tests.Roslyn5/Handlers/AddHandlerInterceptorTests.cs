namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
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
            .Approve()
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
                             [Handler]
                             public class OrderShippedHandler : IHandleMessages<Cmd1>
                             {
                                 public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                             }
                         }
                         
                         public class AnotherOuterClass
                         {
                             public class InnerClass 
                             {
                                  [Handler]
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
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void MixedStyleHandlerProducesNoInterception()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<MixedHandler>();
                         }
                     }

                     public class MixedHandler : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 message, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
                     }

                     public interface IMyService {}
                     public class Cmd1 : ICommand {}
                     public class Cmd2 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InterfaceLessHandler()
    {
        var source = """
                     using System.Threading;
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderShippedHandler>();
                         }
                     }

                     [Handler]
                     public class OrderShippedHandler
                     {
                         public static Task Handle(Cmd1 message, IMessageHandlerContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
                     }

                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InterfaceLessHandlers()
    {
        var source = """
                     using System.Threading;
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using Orders;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderShippedHandler>();
                             cfg.AddHandler<OrderPlacedHandler>();
                             cfg.AddHandler<OrderAcceptedHandler>();
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             public OrderShippedHandler(IMyService service) { }
                             public Task Handle(Cmd1 message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
                         }

                         [Handler]
                         public class OrderPlacedHandler
                         {
                             public static Task Handle(Cmd2 message, IMessageHandlerContext context, CancellationToken ct) => Task.CompletedTask;
                         }
                         
                         [Handler]
                         public class OrderAcceptedHandler
                         {
                            public static Task Handle(Cmd1 message, IMessageHandlerContext context, IMyService service, CancellationToken ct) => Task.CompletedTask;
                         
                             public static Task Handle(Cmd2 message, IMessageHandlerContext context, IMyService service, CancellationToken ct) => Task.CompletedTask;
                         }
                     }

                     public interface IMyService {}
                     public class Cmd1 : ICommand {}
                     public class Cmd2 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InterfaceLessHandlersCtorAndParameterInjection()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using Orders;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderShippedHandler>();
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             public OrderShippedHandler(IServiceA serviceA) { }
                             public Task Handle(Cmd1 message, IMessageHandlerContext context, IServiceB serviceB) => Task.CompletedTask;
                         }
                     }

                     public interface IServiceA {}
                     public interface IServiceB {}
                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InterfaceLessHandlerReturningValueTaskProducesNoInterception()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderShippedHandler>();
                         }
                     }

                     [Handler]
                     public class OrderShippedHandler
                     {
                         public ValueTask Handle(Cmd1 message, IMessageHandlerContext context) => ValueTask.CompletedTask;
                     }

                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InterfaceLessHandlerCtorAndMethodInjectionWithSameParameterName()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using Orders;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderShippedHandler>();
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             public OrderShippedHandler(IServiceA service) { }
                             public Task Handle(Cmd1 message, IMessageHandlerContext context, IServiceB service) => Task.CompletedTask;
                         }
                     }

                     public interface IServiceA {}
                     public interface IServiceB {}
                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InterfaceLessHandlerInheritedFromBaseClass()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using Orders;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderShippedHandler>();
                         }
                     }

                     namespace Orders
                     {
                         public abstract class HandlerBase
                         {
                             public Task Handle(Cmd1 message, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         [Handler]
                         public class OrderShippedHandler : HandlerBase
                         {
                         }
                     }

                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
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
            .Approve()
            .AssertRunsAreEqual();
    }
}