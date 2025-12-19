namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
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
                             new HandlerRegistry(cfg).Orders.AddAll();
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
    public void InterfaceLessHandlers()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             new HandlerRegistry(cfg).Foo.AddAll();
                         }
                     }

                     namespace Foo
                     {
                         [HandlerAttribute]
                         public class InterfaceLess
                         {
                             public Task Handle(MyCommand message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
                         }

                         [HandlerAttribute]
                         public class InterfaceLessNoDependencies
                         {
                             public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     
                         [HandlerAttribute]
                         public class InterfaceLessStatic
                         {
                             public static Task Handle(MyCommand message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     
                         [HandlerAttribute]
                         public class InterfaceLessStaticWithDependencies
                         {
                             public static Task Handle(MyCommand message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
                         }

                         public class MyCommand : ICommand { }
                         public class MyEvent : IEvent { }
                         public interface IMyService { }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .AddReference(MetadataReference.CreateFromFile(typeof(ActivatorUtilities).Assembly.Location))
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }
}
