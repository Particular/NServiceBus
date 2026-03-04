namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using Analyzer.Sagas;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class AddSagaInterceptorTests
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
                             cfg.AddSaga<Orders.Shipping.OrderShippingPolicy>();
                             cfg.AddSaga<Orders.Billing.OrderBillingPolicy>();
                             cfg.AddSaga<Payments.PaymentsPolicy>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddSaga<Payments.PaymentsPolicy>();
                         }
                     }

                     namespace Orders.Shipping
                     {
                         [Saga]
                         public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                             IAmStartedByMessages<OrderPlaced>,
                             IHandleMessages<OrderBilled>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }

                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
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
                             IAmStartedByMessages<OrderBilled>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderBillingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                     .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                     .ToMessage<OrderBilled>(msg => msg.OrderId);
                             }

                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                             public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
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
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
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
                             cfg.AddSaga<Orders.Shipping.OuterClass.OrderShippingPolicy>();
                             cfg.AddSaga<Orders.Shipping.AnotherOuterClass.InnerClass.OrderShippingPolicy>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddSaga<Orders.Shipping.AnotherOuterClass.InnerClass.OrderShippingPolicy>();
                         }
                     }

                     namespace Orders.Shipping
                     {
                         public class OuterClass
                         {
                             [Saga]
                             public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                                 IAmStartedByMessages<OrderPlaced>,
                                 IHandleMessages<OrderBilled>
                             {
                                 protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                                 {
                                     mapper.MapSaga(saga => saga.OrderId)
                                         .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                         .ToMessage<OrderBilled>(msg => msg.OrderId);
                                 }

                                 public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                                 public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
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
                                     IHandleMessages<OrderBilled>
                                 {
                                     protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                                     {
                                         mapper.MapSaga(saga => saga.OrderId)
                                             .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                             .ToMessage<OrderBilled>(msg => msg.OrderId);
                                     }

                                     public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                                     public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void UnrelatedCompilationError()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderShippingPolicy>();
                         }

                         void BreakCompilation()
                         {
                             NotAType x = default; // intentional error
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IAmStartedByMessages<OrderPlaced>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                 .ToMessage<OrderPlaced>(msg => msg.OrderId);
                         }

                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderShippingPolicyData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderPlaced : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .SuppressCompilationErrors()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void PrimaryConstructorAndSyntaxWrappers()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<PartitionedEndpointSaga>();
                         }
                     }

                     [Saga]
                     public class PartitionedEndpointSaga(object logger)
                         : Saga<PartitionedEndpointSagaData>, IAmStartedByMessages<StartPartitionSagaCommand>
                     {
                         public object Logger { get; } = logger;

                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PartitionedEndpointSagaData> mapper)
                         {
                             mapper.MapSaga(saga => (saga).CorrelationId)
                                   .ToMessage<StartPartitionSagaCommand>(m => m!.CorrelationId);
                         }

                         public Task Handle(StartPartitionSagaCommand message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class PartitionedEndpointSagaData : ContainSagaData
                     {
                         public string CorrelationId { get; set; }
                     }

                     public class StartPartitionSagaCommand : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ExpressionBodiedConfigureHowToFindSaga()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderShippingPolicy>();
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IAmStartedByMessages<OrderPlaced>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper) =>
                             mapper.MapSaga(saga => (saga).OrderId)
                                   .ToMessage<OrderPlaced>(msg => msg!.OrderId);

                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderShippingPolicyData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderPlaced : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InvalidMappingWithCompilationErrors()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderShippingPolicy>();
                         }

                         void BreakCompilation()
                         {
                             NotAType x = default; // intentional error
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IAmStartedByMessages<OrderPlaced>, IHandleMessages<OrderBilled>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                 .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                 .ToMessage<OrderBilled>(msg => msg?.OrderId);
                         }

                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
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
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .SuppressCompilationErrors()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void CastSyntaxWrappers()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderShippingPolicy>();
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IAmStartedByMessages<OrderPlaced>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => ((OrderShippingPolicyData)saga).OrderId)
                                 .ToMessage<OrderPlaced>(msg => ((OrderPlaced)msg).OrderId);
                         }

                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderShippingPolicyData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderPlaced : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void UnrelatedCompilationErrorInDifferentFile()
    {
        var setupSource = """
                          using NServiceBus;

                          public class Test
                          {
                              public void Configure(EndpointConfiguration cfg)
                              {
                                  cfg.AddSaga<OrderShippingPolicy>();
                              }

                              void BreakCompilation()
                              {
                                  NotAType x = default; // intentional error in different file
                              }
                          }
                          """;

        var sagaSource = """
                         using System.Threading.Tasks;
                         using NServiceBus;

                         [Saga]
                         public class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IAmStartedByMessages<OrderPlaced>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                             {
                                 mapper.MapSaga(saga => saga.OrderId)
                                       .ToMessage<OrderPlaced>(msg => msg.OrderId);
                             }

                             public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         public class OrderShippingPolicyData : ContainSagaData
                         {
                             public string OrderId { get; set; }
                         }

                         public class OrderPlaced : IEvent
                         {
                             public string OrderId { get; set; }
                         }
                         """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(setupSource, "setup.cs")
            .WithSource(sagaSource, "saga.cs")
            .SuppressCompilationErrors()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void DuplicatePropertyDefinitionsWithCompilationErrors()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderShippingPolicy>();
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IAmStartedByMessages<OrderPlaced>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                   .ToMessage<OrderPlaced>(msg => msg.OrderId);
                         }

                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderShippingPolicyData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public partial class OrderPlaced : IEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public partial class OrderPlaced
                     {
                         public string OrderId { get; set; } // intentional duplicate member error
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .SuppressCompilationErrors()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NullableReferenceTypeMappings()
    {
        var source = """
                     #nullable enable
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderShippingPolicy>();
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IAmStartedByMessages<OrderPlaced>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                   .ToMessage<OrderPlaced>(msg => msg.OrderId);
                         }

                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderShippingPolicyData : ContainSagaData
                     {
                         public string? OrderId { get; set; }
                     }

                     public class OrderPlaced : IEvent
                     {
                         public string? OrderId { get; set; }
                     }
                     #nullable restore
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NullableReferenceTypeMixedMappings()
    {
        var source = """
                     #nullable enable
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<NullableSaga>();
                             cfg.AddSaga<NonNullableSaga>();
                         }
                     }

                     [Saga]
                     public class NullableSaga : Saga<NullableSagaData>, IAmStartedByMessages<NonNullableMessage>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NullableSagaData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                   .ToMessage<NonNullableMessage>(msg => msg.OrderId);
                         }

                         public Task Handle(NonNullableMessage evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class NullableSagaData : ContainSagaData
                     {
                         public string? OrderId { get; set; }
                     }

                     public class NonNullableMessage : IEvent
                     {
                         public string OrderId { get; set; } = string.Empty;
                     }

                     [Saga]
                     public class NonNullableSaga : Saga<NonNullableSagaData>, IAmStartedByMessages<NullableMessage>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NonNullableSagaData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                   .ToMessage<NullableMessage>(msg => msg.OrderId);
                         }

                         public Task Handle(NullableMessage evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class NonNullableSagaData : ContainSagaData
                     {
                         public string OrderId { get; set; } = string.Empty;
                     }

                     public class NullableMessage : IEvent
                     {
                         public string? OrderId { get; set; }
                     }
                     #nullable restore
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InheritedMessageProperty()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public abstract class BaseEvent
                     {
                         public string OrderId { get; set; }
                     }

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<OrderShippingPolicy>();
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                         IAmStartedByMessages<OrderPlaced>,
                         IHandleMessages<OrderBilled>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                   .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                   .ToMessage<OrderBilled>(msg => msg.OrderId);
                         }

                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderShippingPolicyData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class OrderPlaced : BaseEvent, IEvent { }

                     public class OrderBilled : BaseEvent, IEvent { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }
}