namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Sagas;
using NUnit.Framework;
using Particular.AnalyzerTesting;

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
                             cfg.Handlers.SagaMappingStillWorksWithUnrelatedCompilationErrorAssembly.AddAll();
                         }

                         void BreakCompilation()
                         {
                             NotAType x = default; // intentional error
                         }
                     }

                     namespace Orders.Shipping
                     {
                         [Saga]
                         public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                             IAmStartedByMessages<OrderPlaced>
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
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                             cfg.Handlers.PrimaryConstructorAndSyntaxWrappersAssembly.AddAll();
                         }
                     }

                     namespace Orders.Shipping
                     {
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
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                             cfg.Handlers.ExpressionBodiedConfigureHowToFindSagaAssembly.AddAll();
                         }
                     }

                     [Saga]
                     public class OrderShippingPolicy : Saga<OrderShippingPolicyData>,
                         IAmStartedByMessages<OrderPlaced>
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                             cfg.Handlers.InvalidMappingWithCompilationErrorsAssembly.AddAll();
                         }

                         void BreakCompilation()
                         {
                             NotAType x = default; // intentional error
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                             cfg.Handlers.CastSyntaxWrappersAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                                  cfg.Handlers.UnrelatedCompilationErrorInDifferentFileAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                             cfg.Handlers.DuplicatePropertyDefinitionsWithCompilationErrorsAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                             cfg.Handlers.NullableReferenceTypeMappingsAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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
                             cfg.Handlers.NullableReferenceTypeMixedMappingsAssembly.AddAll();
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
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

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.InheritedMessagePropertyAssembly.AddAll();
                         }
                     }

                     public abstract class BaseEvent
                     {
                         public string OrderId { get; set; }
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

        SourceGeneratorTest.ForIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Approve()
            .AssertRunsAreEqual();
    }
}