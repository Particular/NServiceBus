namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

// When a handler named "Order" and another named "OrderHandler" both exist,
// the default naming convention produces "AddOrderHandler" for both:
//   "Order" -> "Add" + "Order" + "Handler" suffix = AddOrderHandler
//   "OrderHandler" -> "Add" + "OrderHandler" = AddOrderHandler
// This collision can only be resolved by explicitly registering a custom
// RegistrationMethodNamePattern via [HandlerRegistryExtensions]. We intentionally
// do not auto-prefix or auto-postfix generated registration method names because
// doing so could lead to unpredictable behavior and break the stable API surface
// that users rely on for selective handler registration.
public class AddHandlerGeneratorCollisionTests
{
    [Test]
    public void HandlerNameCollisionResolvedByRegistrationMethodNamePatterns()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using CustomRegistrations;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollisionDemo.AddAll();
                         }
                     }

                     namespace CustomRegistrations
                     {
                         [HandlerRegistryExtensions(EntryPointName = "CollisionDemo", RegistrationMethodNamePatterns = ["^OrderHandler$=>AddOrderMessageHandler"])]
                         internal static partial class MyCustomHandlerRegistryExtensions;
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class Order : IHandleMessages<OrderPlaced>
                         {
                             public Task Handle(OrderPlaced message, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         [Handler]
                         public class OrderHandler : IHandleMessages<OrderCancelled>
                         {
                             public Task Handle(OrderCancelled message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class OrderPlaced : IEvent;
                     public class OrderCancelled : IEvent;
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void HandlerNameCollisionNotResolvedDoesNotCompile()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using CustomRegistrations;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollisionDemo.AddAll();
                         }
                     }

                     namespace CustomRegistrations
                     {
                         [HandlerRegistryExtensions(EntryPointName = "CollisionDemo")]
                         internal static partial class MyCustomHandlerRegistryExtensions;
                     }

                     namespace Orders
                     {
                         [Handler]
                         public class Order : IHandleMessages<OrderPlaced>
                         {
                             public Task Handle(OrderPlaced message, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         [Handler]
                         public class OrderHandler : IHandleMessages<OrderCancelled>
                         {
                             public Task Handle(OrderCancelled message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class OrderPlaced : IEvent;
                     public class OrderCancelled : IEvent;
                     """;

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .SuppressCompilationErrors()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Contain("CS0111").And.Contain("Type 'MyCustomHandlerRegistryExtensions.MyCustomRootRegistry.OrdersRegistry' already defines a member called 'AddOrderHandler' with the same parameter types"));
    }
}