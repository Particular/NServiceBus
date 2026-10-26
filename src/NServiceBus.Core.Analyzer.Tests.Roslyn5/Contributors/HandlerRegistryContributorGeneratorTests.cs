namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using Analyzer.Handlers;
using Analyzer.Sagas;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class HandlerRegistryContributorGeneratorTests
{
    const string ContributorSource = """
                                     [HandlerRegistryContributor]
                                     internal sealed class {0} : IHandlerRegistryContributor<{0}>
                                     {
                                         public static {0} Instance { get; } = new();

                                         public void AddHandlers(EndpointConfiguration configuration) { }
                                         public void AddSagas(EndpointConfiguration configuration) { }
                                     }
                                     """;

    static string Contributor(string name) => ContributorSource.Replace("{0}", name);

    [Test]
    public void ContributorAtRootNode()
    {
        var source = $$"""
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg) => cfg.Handlers.ContributorAtRootNodeAssembly.AddAll();
                       }

                       {{Contributor("RootContributor")}}
                       """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ContributorInNestedNamespace()
    {
        var source = $$"""
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg) => cfg.Handlers.ContributorInNestedNamespaceAssembly.Acme.Workflows.Orders.AddAll();
                       }

                       namespace Acme.Workflows.Orders
                       {
                           {{Contributor("OrderWorkflowContributor")}}
                       }
                       """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ContributorInNServiceBusNamespaceDropsLeadingSegment()
    {
        var source = $$"""
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg) => cfg.Handlers.ContributorInNServiceBusNamespaceDropsLeadingSegmentAssembly.Workflows.AddAll();
                       }

                       namespace NServiceBus.Workflows
                       {
                           {{Contributor("WorkflowContributor")}}
                       }
                       """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ContributorOnlyAssemblyFromPostInitializationOutput()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) => cfg.Handlers.ContributorOnlyAssemblyFromPostInitializationOutputAssembly.Acme.Workflows.AddAll();
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithIncrementalGenerator<ContributorFixtureGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void ContributorWithHandlersAndSagasInSameNode()
    {
        var source = $$"""
                       using System.Threading.Tasks;
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg) => cfg.Handlers.ContributorWithHandlersAndSagasInSameNodeAssembly.AddAll();
                       }

                       namespace Acme.Orders
                       {
                           {{Contributor("OrderWorkflowContributor")}}

                           [Handler]
                           public class ShipOrderHandler : IHandleMessages<ShipOrder>
                           {
                               public Task Handle(ShipOrder message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           [Saga]
                           public class OrderPolicy : Saga<OrderPolicyData>, IAmStartedByMessages<StartOrder>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderPolicyData> mapper) =>
                                   mapper.MapSaga(s => s.OrderId).ToMessage<StartOrder>(m => m.OrderId);

                               public Task Handle(StartOrder message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class OrderPolicyData : ContainSagaData
                           {
                               public string OrderId { get; set; }
                           }

                           public class ShipOrder : ICommand { }

                           public class StartOrder : ICommand
                           {
                               public string OrderId { get; set; }
                           }
                       }
                       """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithIncrementalGenerator<AddSagaGenerator>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void MultipleContributorsAreOrderedDeterministically()
    {
        var source = $$"""
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg) => cfg.Handlers.MultipleContributorsAreOrderedDeterministicallyAssembly.AddAll();
                       }

                       namespace Zeta
                       {
                           {{Contributor("ZetaContributor")}}
                       }

                       {{Contributor("RootContributor")}}

                       namespace Acme
                       {
                           {{Contributor("SecondContributor")}}

                           {{Contributor("FirstContributor")}}
                       }
                       """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NestedContributorUsesTheNamespaceOfItsContainingType()
    {
        var source = $$"""
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg) => cfg.Handlers.NestedContributorUsesTheNamespaceOfItsContainingTypeAssembly.Acme.AddAll();
                       }

                       namespace Acme
                       {
                           public static class Holder
                           {
                               {{Contributor("NestedContributor")}}
                           }
                       }
                       """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void RecordClassContributorIsCollected()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryContributor]
                     internal sealed record class RecordContributor : IHandlerRegistryContributor<RecordContributor>
                     {
                         public static RecordContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        var output = GeneratedCompilation.Run(nameof(RecordClassContributorIsCollected), source, [new AddHandlerAndSagasRegistrationGenerator()]);

        Assert.That(output.CompilationErrors, Is.Empty);
        Assert.That(output.SourceOf("HandlerRegistrations.g.cs"), Does.Contain("AddHandlers<global::RecordContributor>(_configuration);").And.Contain("AddSagas<global::RecordContributor>(_configuration);"));
    }

    [Test]
    public void NoContributorsAndNoHandlersProduceNoOutput()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) { }
                     }
                     """;

        var output = GeneratedCompilation.Run(nameof(NoContributorsAndNoHandlersProduceNoOutput), source, [new AddHandlerAndSagasRegistrationGenerator()]);

        Assert.That(output.Sources, Is.Empty);
    }

    [Test]
    public void InvalidContributorsAreIgnored()
    {
        var source = """
                     using NServiceBus;

                     public class NotAContributor { }

                     [HandlerRegistryContributor]
                     public class DoesNotImplementTheInterface
                     {
                         public static DoesNotImplementTheInterface Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }

                     [HandlerRegistryContributor]
                     public class GenericContributor<T> : IHandlerRegistryContributor<GenericContributor<T>>
                     {
                         public static GenericContributor<T> Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }

                     public static class Holder
                     {
                         [HandlerRegistryContributor]
                         class PrivateContributor : IHandlerRegistryContributor<PrivateContributor>
                         {
                             public static PrivateContributor Instance { get; } = new();

                             public void AddHandlers(EndpointConfiguration configuration) { }
                             public void AddSagas(EndpointConfiguration configuration) { }
                         }
                     }
                     """;

        var output = GeneratedCompilation.Run(nameof(InvalidContributorsAreIgnored), source, [new AddHandlerAndSagasRegistrationGenerator()]);

        Assert.That(output.Sources, Is.Empty);
    }
}
