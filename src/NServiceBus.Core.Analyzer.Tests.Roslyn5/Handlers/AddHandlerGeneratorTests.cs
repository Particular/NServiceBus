namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
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
                         [Handler]
                         public class OrderShippedHandler : IHandleMessages<Cmd1>
                         {
                             public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         [Handler]
                         public class ShipmentDelayedHandler : IHandleMessages<Cmd2>
                         {
                             public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     namespace Orders.Billing
                     {
                         [Handler]
                         public class InvoiceCreatedHandler : IHandleMessages<Evt1>
                         {
                             public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     namespace Payments
                     {
                         [Handler]
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
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void RootClassVisibilityAndNamespace()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using CustomRegistrations;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.RootClassVisibilityAndNamespaceAssembly.AddAll();
                         }
                     }

                     namespace CustomRegistrations
                     {
                         [HandlerRegistryExtensions]
                         internal static partial class MyCustomHandlerRegistryExtensions
                         {
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler : IHandleMessages<Cmd1>
                         {
                             public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class Cmd1 : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void RootClassEntryPointName()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using CustomRegistrations;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CustomEntryPoint.AddAll();
                         }
                     }

                     namespace CustomRegistrations
                     {
                        [HandlerRegistryExtensions(EntryPointName = "CustomEntryPoint")]
                        internal static partial class MyCustomHandlerRegistryExtensions
                        {
                        }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler : IHandleMessages<Cmd1>
                         {
                             public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class Cmd1 : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.InterfaceLessHandlersAssembly.AddAll();
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
                     }

                     public interface IMyService {}
                     public class Cmd1 : ICommand {}
                     public class Cmd2 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.InterfaceLessHandlersCtorAndParameterInjectionAssembly.AddAll();
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             // IServiceA injected via constructor
                             public OrderShippedHandler(IServiceA serviceA) { }
                             // IServiceB injected via method parameter — different from ctor dep
                             public Task Handle(Cmd1 message, IMessageHandlerContext context, IServiceB serviceB) => Task.CompletedTask;
                         }
                     }

                     public interface IServiceA {}
                     public interface IServiceB {}
                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void MixedStyleHandlerProducesNoRegistration()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.MixedStyleHandlerProducesNoRegistrationAssembly.AddAll();
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class MixedHandler : IHandleMessages<Cmd1>
                         {
                             public Task Handle(Cmd1 message, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(Cmd2 message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
                         }
                     }

                     public interface IMyService {}
                     public class Cmd1 : ICommand {}
                     public class Cmd2 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void RegistrationMethodNamePatterns()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using CustomRegistrations;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.RegistrationMethodNamePatternsAssembly.AddAll();
                         }
                     }

                     namespace CustomRegistrations
                     {
                         [HandlerRegistryExtensions(RegistrationMethodNamePatterns = ["^NoMatch$=>Ignored", "Handler$=>Register"])]
                         internal static partial class MyCustomHandlerRegistryExtensions
                         {
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler : IHandleMessages<Cmd1>
                         {
                             public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class Cmd1 : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }
}