namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using Helpers;
using NUnit.Framework;

public class AddHandlerGeneratorTests
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
                             cfg.Handlers.BasicHandlersAssembly.AddAll();
                         }
                     }

                     namespace Orders.Shipping
                     {
                         [HandlerAttribute]
                         public class OrderShippedHandler : IHandleMessages<Cmd1>
                         {
                             public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         [HandlerAttribute]
                         public class ShipmentDelayedHandler : IHandleMessages<Cmd2>
                         {
                             public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     namespace Orders.Billing
                     {
                         [HandlerAttribute]
                         public class InvoiceCreatedHandler : IHandleMessages<Evt1>
                         {
                             public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     namespace Payments
                     {
                         [HandlerAttribute]
                         public class PaymentCapturedHandler : IHandleMessages<Cmd1>
                         {
                             public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NestedHandler()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.NestedHandlerAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }
}