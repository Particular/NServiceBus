namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Analyzer.Handlers;
using Analyzer.Sagas;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class GeneratedHandlerRegistryEntryTests
{
    [Test]
    public void EntryIsInternalWhenTheRegistryIsInternal()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) => cfg.Handlers.Orders.AddAll();
                     }

                     [HandlerRegistryExtensions(EntryPointName = "Orders")]
                     internal static partial class OrdersRegistrations
                     {
                     }

                     [HandlerRegistryContributor]
                     internal sealed class OrdersContributor : IHandlerRegistryContributor<OrdersContributor>
                     {
                         public static OrdersContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void EntryReferencesRootClassInTheGlobalNamespace()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) => cfg.Handlers.Orders.AddAll();
                     }

                     [HandlerRegistryExtensions(EntryPointName = "Orders")]
                     public static partial class OrdersRegistrations
                     {
                     }

                     [HandlerRegistryContributor]
                     internal sealed class OrdersContributor : IHandlerRegistryContributor<OrdersContributor>
                     {
                         public static OrdersContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NoEntryOrAssemblyAttributeIsEmittedWhenTheAssemblyOptsOutOfComposition()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryContributor]
                     internal sealed class OrdersContributor : IHandlerRegistryContributor<OrdersContributor>
                     {
                         public static OrdersContributor Instance { get; } = new();

                         public void AddHandlers(EndpointConfiguration configuration) { }
                         public void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithProperty("build_property.NServiceBusHandlerRegistryComposition", "false")
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [TestCase("false")]
    [TestCase("False")]
    [TestCase("FALSE")]
    public void OptOutIsCaseInsensitive(string value)
    {
        var output = GeneratedCompilation.Run("OptOut", ContributorOnlySource, [new AddHandlerAndSagasRegistrationGenerator()],
            new Dictionary<string, string> { ["build_property.NServiceBusHandlerRegistryComposition"] = value });

        Assert.That(output.Sources.Select(s => s.HintName), Is.EqualTo(["HandlerRegistrations.g.cs"]));
    }

    [TestCase("true")]
    [TestCase("")]
    [TestCase("garbage")]
    public void EntryIsEmittedUnlessThePropertyIsFalse(string value)
    {
        var output = GeneratedCompilation.Run("Participating", ContributorOnlySource, [new AddHandlerAndSagasRegistrationGenerator()],
            new Dictionary<string, string> { ["build_property.NServiceBusHandlerRegistryComposition"] = value });

        Assert.That(output.Sources.Select(s => s.HintName), Is.EqualTo(["HandlerRegistrations.g.cs", "HandlerRegistryEntry.g.cs"]));
    }

    [Test]
    public void OptingOutKeepsTheAssemblyRegistry()
    {
        var output = GeneratedCompilation.Run(nameof(OptingOutKeepsTheAssemblyRegistry), InternalRegistrySource, [new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()], GeneratedCompilation.OptOutOfComposition);
        Assert.That(output.CompilationErrors, Is.Empty);

        var assembly = GeneratedCompilation.Load(output.Compilation);
        Assert.That(assembly.GetCustomAttribute<GeneratedHandlerRegistryAttribute>(), Is.Null);
        Assert.That(assembly.GetType("NServiceBus.Generated." + nameof(OptingOutKeepsTheAssemblyRegistry) + "HandlerRegistryEntry"), Is.Null);
        Assert.That(output.SourceOf("HandlerRegistrations.g.cs"), Does.Contain("OrdersRegistrations"));
    }

    [Test]
    public void EntryHasAStableHintNameAndNameFromTheAssembly()
    {
        var output = GeneratedCompilation.Run("My.Orders-Assembly", ContributorOnlySource, [new AddHandlerAndSagasRegistrationGenerator()]);

        Assert.That(output.Sources.Select(s => s.HintName), Is.EqualTo(["HandlerRegistrations.g.cs", "HandlerRegistryEntry.g.cs"]));
        Assert.That(output.SourceOf("HandlerRegistryEntry.g.cs"), Does.Match(@"public static class MyOrdersAssemblyHandlerRegistryEntry_[0-9a-f]{16}\b"));
    }

    [Test]
    public void EntryForwardsToTheRootRegistryWhenItIsInternal()
    {
        var output = GeneratedCompilation.Run(nameof(EntryForwardsToTheRootRegistryWhenItIsInternal), InternalRegistrySource, [new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()]);
        Assert.That(output.CompilationErrors, Is.Empty);

        var assembly = GeneratedCompilation.Load(output.Compilation);
        var attribute = assembly.GetCustomAttribute<GeneratedHandlerRegistryAttribute>();

        Assert.That(attribute, Is.Not.Null);
        var entryType = attribute.EntryType;
        Assert.That(!entryType.IsPublic && entryType.IsAbstract && entryType.IsSealed, Is.True, "The entry must be an internal static class.");
        Assert.That(entryType.Namespace, Is.EqualTo("NServiceBus.Generated"));
        Assert.That(entryType.GetCustomAttribute<EditorBrowsableAttribute>()?.State, Is.EqualTo(EditorBrowsableState.Never));

        var recorded = (List<string>)assembly.GetType("Calls")!.GetField("Recorded")!.GetValue(null)!;
        var configuration = new EndpointConfiguration("Entry");

        entryType.GetMethod("AddAllHandlers")!.Invoke(null, [configuration]);
        Assert.That(string.Join(", ", recorded), Is.EqualTo("AddHandlers"));

        recorded.Clear();
        entryType.GetMethod("AddAllSagas")!.Invoke(null, [configuration]);
        Assert.That(string.Join(", ", recorded), Is.EqualTo("AddSagas"));

        recorded.Clear();
        entryType.GetMethod("AddAll")!.Invoke(null, [configuration]);
        Assert.That(string.Join(", ", recorded), Is.EqualTo("AddHandlers, AddSagas"));
    }

    const string ContributorOnlySource = """
                                         using NServiceBus;

                                         [HandlerRegistryContributor]
                                         internal sealed class OrdersContributor : IHandlerRegistryContributor<OrdersContributor>
                                         {
                                             public static OrdersContributor Instance { get; } = new();

                                             public void AddHandlers(EndpointConfiguration configuration) { }
                                             public void AddSagas(EndpointConfiguration configuration) { }
                                         }
                                         """;

    const string InternalRegistrySource = """
                                          using System.Collections.Generic;
                                          using NServiceBus;

                                          public static class Calls
                                          {
                                              public static readonly List<string> Recorded = [];
                                          }

                                          [HandlerRegistryExtensions(EntryPointName = "Orders")]
                                          internal static partial class OrdersRegistrations
                                          {
                                          }

                                          [HandlerRegistryContributor]
                                          internal sealed class OrdersContributor : IHandlerRegistryContributor<OrdersContributor>
                                          {
                                              public static OrdersContributor Instance { get; } = new();

                                              public void AddHandlers(EndpointConfiguration configuration) => Calls.Recorded.Add("AddHandlers");
                                              public void AddSagas(EndpointConfiguration configuration) => Calls.Recorded.Add("AddSagas");
                                          }
                                          """;
}
