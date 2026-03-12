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
    public void ConventionBasedHandlers()
    {
        var source = """
                     using System.Threading;
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlersAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ConventionBasedHandlersCtorAndParameterInjection()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlersCtorAndParameterInjectionAssembly.AddAll();
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
    public void ConventionBasedHandlerWithMultipleConstructorsUsesMostGreedyConstructor()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlerWithMultipleConstructorsUsesMostGreedyConstructorAssembly.AddAll();
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             public OrderShippedHandler() { }
                             public OrderShippedHandler(IServiceA serviceA) { }
                             public OrderShippedHandler(IServiceA serviceA, IServiceB serviceB) { }

                             public Task Handle(Cmd1 message, IMessageHandlerContext context, IServiceC serviceC) => Task.CompletedTask;
                         }
                     }

                     public interface IServiceA {}
                     public interface IServiceB {}
                     public interface IServiceC {}
                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ConventionBasedHandlerWithActivatorUtilitiesConstructorAttributeUsesMarkedConstructor()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlerWithActivatorUtilitiesConstructorAttributeUsesMarkedConstructorAssembly.AddAll();
                         }
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             public OrderShippedHandler(IServiceA serviceA, IServiceB serviceB) { }

                             [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
                             public OrderShippedHandler(IServiceC serviceC) { }

                             public Task Handle(Cmd1 message, IMessageHandlerContext context, IServiceD serviceD) => Task.CompletedTask;
                         }
                     }

                     public interface IServiceA {}
                     public interface IServiceB {}
                     public interface IServiceC {}
                     public interface IServiceD {}
                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ConventionBasedHandlerReturningValueTaskProducesNoRegistration()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlerReturningValueTaskProducesNoRegistrationAssembly.AddAll();
                         }
                     }

                     [Handler]
                     public class OrderShippedHandler
                     {
                         public ValueTask Handle(Cmd1 message, IMessageHandlerContext context) => ValueTask.CompletedTask;
                     }

                     public class Cmd1 : ICommand {}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ConventionBasedHandlerCtorAndMethodInjectionWithSameParameterName()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlerCtorAndMethodInjectionWithSameParameterNameAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ConventionBasedHandlerWithConflictingCtorParameterName()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlerWithConflictingCtorParameterNameAssembly.AddAll();
                         }
                     }

                     public interface IServiceA {}
                     public interface IServiceB {}
                     public class Cmd1 : ICommand {}

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             public OrderShippedHandler(IServiceA service, IServiceB serviceFromCtor) { }
                             public Task Handle(Cmd1 message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ConventionBasedHandlerWithSuffixCollision()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlerWithSuffixCollisionAssembly.AddAll();
                         }
                     }

                     public interface IServiceA {}
                     public interface IServiceB {}
                     public interface IServiceC {}
                     public class Cmd1 : ICommand {}

                     namespace Orders
                     {
                         [Handler]
                         public class OrderShippedHandler
                         {
                             public OrderShippedHandler(IServiceA service, IServiceB serviceFromCtor, IServiceC serviceFromCtorFromCtor) { }
                             public Task Handle(Cmd1 message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ConventionBasedHandlerInheritedFromBaseClass()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.ConventionBasedHandlerInheritedFromBaseClassAssembly.AddAll();
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