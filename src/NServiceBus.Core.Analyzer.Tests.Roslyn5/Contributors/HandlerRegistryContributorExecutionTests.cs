namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using System.Collections.Generic;
using System.Reflection;
using Analyzer.Handlers;
using Analyzer.Sagas;
using NUnit.Framework;

[TestFixture]
public class HandlerRegistryContributorExecutionTests
{
    const string ContributorsSource = """
                                      using System.Collections.Generic;
                                      using NServiceBus;

                                      public static class Calls
                                      {
                                          public static readonly List<string> Recorded = [];
                                      }

                                      [HandlerRegistryContributor]
                                      internal sealed class RootContributor : IHandlerRegistryContributor<RootContributor>
                                      {
                                          public static RootContributor Instance { get; } = new();

                                          public void AddHandlers(EndpointConfiguration configuration) => Calls.Recorded.Add("Root.AddHandlers");
                                          public void AddSagas(EndpointConfiguration configuration) => Calls.Recorded.Add("Root.AddSagas");
                                      }

                                      namespace Acme.Workflows
                                      {
                                          [HandlerRegistryContributor]
                                          internal sealed class WorkflowContributor : IHandlerRegistryContributor<WorkflowContributor>
                                          {
                                              public static WorkflowContributor Instance { get; } = new();

                                              public void AddHandlers(EndpointConfiguration configuration) => Calls.Recorded.Add("Workflow.AddHandlers");
                                              public void AddSagas(EndpointConfiguration configuration) => Calls.Recorded.Add("Workflow.AddSagas");
                                          }
                                      }

                                      namespace Acme.NonGeneric
                                      {
                                          [HandlerRegistryContributor]
                                          internal sealed class NonGenericContributor : IHandlerRegistryContributor
                                          {
                                              public static IHandlerRegistryContributor Instance { get; } = new NonGenericContributor();

                                              public void AddHandlers(EndpointConfiguration configuration) => Calls.Recorded.Add("NonGeneric.AddHandlers");
                                              public void AddSagas(EndpointConfiguration configuration) => Calls.Recorded.Add("NonGeneric.AddSagas");
                                          }
                                      }

                                      namespace Acme.Explicit
                                      {
                                          [HandlerRegistryContributor]
                                          internal sealed class ExplicitContributor : IHandlerRegistryContributor<ExplicitContributor>
                                          {
                                              static ExplicitContributor IHandlerRegistryContributor<ExplicitContributor>.Instance { get; } = new();

                                              void IHandlerRegistryContributor.AddHandlers(EndpointConfiguration configuration) => Calls.Recorded.Add("Explicit.AddHandlers");
                                              void IHandlerRegistryContributor.AddSagas(EndpointConfiguration configuration) => Calls.Recorded.Add("Explicit.AddSagas");
                                          }
                                      }
                                      """;

    [Test]
    public void AddAllRunsContributorsOfTheNodeAndItsChildren()
    {
        var calls = Execute(nameof(AddAllRunsContributorsOfTheNodeAndItsChildren), "cfg.Handlers.AddAllRunsContributorsOfTheNodeAndItsChildrenAssembly.AddAll();");

        Assert.That(calls, Is.EqualTo("Root.AddHandlers, Explicit.AddHandlers, NonGeneric.AddHandlers, Workflow.AddHandlers, Root.AddSagas, Explicit.AddSagas, NonGeneric.AddSagas, Workflow.AddSagas"));
    }

    [Test]
    public void AddAllHandlersRunsOnlyHandlerContributions()
    {
        var calls = Execute(nameof(AddAllHandlersRunsOnlyHandlerContributions), "cfg.Handlers.AddAllHandlersRunsOnlyHandlerContributionsAssembly.AddAllHandlers();");

        Assert.That(calls, Is.EqualTo("Root.AddHandlers, Explicit.AddHandlers, NonGeneric.AddHandlers, Workflow.AddHandlers"));
    }

    [Test]
    public void ChildNodeRunsOnlyItsOwnContributors()
    {
        var calls = Execute(nameof(ChildNodeRunsOnlyItsOwnContributors), "cfg.Handlers.ChildNodeRunsOnlyItsOwnContributorsAssembly.Acme.Workflows.AddAll();");

        Assert.That(calls, Is.EqualTo("Workflow.AddHandlers, Workflow.AddSagas"));
    }

    [Test]
    public void ContributorImplementingOnlyTheNonGenericInterfaceIsCalled()
    {
        var calls = Execute(nameof(ContributorImplementingOnlyTheNonGenericInterfaceIsCalled), "cfg.Handlers.ContributorImplementingOnlyTheNonGenericInterfaceIsCalledAssembly.Acme.NonGeneric.AddAll();");

        Assert.That(calls, Is.EqualTo("NonGeneric.AddHandlers, NonGeneric.AddSagas"));
    }

    [Test]
    public void ExplicitInterfaceImplementationsAreCalled()
    {
        var calls = Execute(nameof(ExplicitInterfaceImplementationsAreCalled), "cfg.Handlers.ExplicitInterfaceImplementationsAreCalledAssembly.Acme.Explicit.AddAll();");

        Assert.That(calls, Is.EqualTo("Explicit.AddHandlers, Explicit.AddSagas"));
    }

    static string Execute(string assemblyName, string statement)
    {
        var source = $$"""
                       {{ContributorsSource}}

                       public class Test
                       {
                           public void Configure(NServiceBus.EndpointConfiguration cfg) { {{statement}} }
                       }
                       """;

        var output = GeneratedCompilation.Run(assemblyName, source, [new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()]);
        Assert.That(output.CompilationErrors, Is.Empty);

        var assembly = GeneratedCompilation.Load(output.Compilation);
        var configuration = new EndpointConfiguration("Contributors");
        var test = assembly.GetType("Test")!;
        test.GetMethod("Configure")!.Invoke(System.Activator.CreateInstance(test), [configuration]);

        var recorded = (List<string>)assembly.GetType("Calls")!.GetField("Recorded", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        return string.Join(", ", recorded);
    }
}
