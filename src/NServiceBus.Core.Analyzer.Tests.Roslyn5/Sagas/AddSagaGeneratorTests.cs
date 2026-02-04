namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Sagas;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class AddSagaGeneratorTests
{
    [Test]
    public void BasicSagas()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.BasicSagasAssembly.AddAll();
                         }
                     }

                     namespace Orders.Shipping
                     {
                         [Saga]
                         public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                             IAmStartedByMessages<OrderPlaced>,
                             IHandleMessages<OrderBilled>,
                             IHandleTimeouts<OrderPlaced>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }
                             
                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                         
                         public class OrderShippingPolicyData : ContainSagaData
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
                     }

                     namespace Orders.Billing
                     {
                         [Saga]
                         public class OrderBillingPolicy : Saga<OrderBillingPolicyData>,
                             IAmStartedByMessages<OrderPlaced>,
                             IAmStartedByMessages<OrderBilled>,
                             IHandleTimeouts<OrderPlaced>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderBillingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }
                             
                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                         
                         public class OrderBillingPolicyData : ContainSagaData
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
                     }

                     namespace Payments
                     {
                         [Saga]
                         public class PaymentsPolicy : Saga<PaymentsPolicyData>,
                             IAmStartedByMessages<OrderPlaced>,
                             IHandleMessages<OrderBilled>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PaymentsPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }
                             
                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                         
                         public class PaymentsPolicyData : ContainSagaData
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
                     }

                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NestedSagas()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.NestedSagasAssembly.AddAll();
                         }
                     }

                     namespace Orders.Shipping
                     {
                         public class OuterClass
                         {
                             [Saga]
                             public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                                 IAmStartedByMessages<OrderPlaced>,
                                 IHandleMessages<OrderBilled>,
                                 IHandleTimeouts<OrderPlaced>
                             {
                                 protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                                 {
                                     mapper.MapSaga(saga => saga.OrderId)
                                         .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                         .ToMessage<OrderBilled>(msg => msg.OrderId);
                                 }
                                 
                                 public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                                 public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                                 public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             }
                             
                             public class OrderShippingPolicyData : ContainSagaData
                             {
                                 public string OrderId { get; set; }
                             }
                         }
                         
                         public class AnotherOuterClass
                         {
                             public class InnerClass 
                             {
                                 [Saga]
                                 public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                                     IAmStartedByMessages<OrderPlaced>,
                                     IHandleMessages<OrderBilled>,
                                     IHandleTimeouts<OrderPlaced>
                                 {
                                     protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                                     {
                                         mapper.MapSaga(saga => saga.OrderId)
                                             .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                             .ToMessage<OrderBilled>(msg => msg.OrderId);
                                     }
                                     
                                     public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                                     public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                                     public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                                 }
                                 
                                 public class OrderShippingPolicyData : ContainSagaData
                                 {
                                     public string OrderId { get; set; }
                                 }
                             }
                         }
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
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
                         [Saga]
                         public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                             IAmStartedByMessages<OrderPlaced>,
                             IHandleMessages<OrderBilled>,
                             IHandleTimeouts<OrderPlaced>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }
                             
                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                         
                         public class OrderShippingPolicyData : ContainSagaData
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
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
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
                         [Saga]
                         public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                             IAmStartedByMessages<OrderPlaced>,
                             IHandleMessages<OrderBilled>,
                             IHandleTimeouts<OrderPlaced>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }
                             
                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                         
                         public class OrderShippingPolicyData : ContainSagaData
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
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
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
                         [HandlerRegistryExtensions(RegistrationMethodNamePatterns = ["^NoMatch$=>Ignored", "Policy$=>Flow"])]
                         internal static partial class MyCustomHandlerRegistryExtensions
                         {
                         }
                     }

                     namespace Orders
                     {
                         [Saga]
                         public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                             IAmStartedByMessages<OrderPlaced>,
                             IHandleMessages<OrderBilled>,
                             IHandleTimeouts<OrderPlaced>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }
                             
                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                         
                         public class OrderShippingPolicyData : ContainSagaData
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
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }
}