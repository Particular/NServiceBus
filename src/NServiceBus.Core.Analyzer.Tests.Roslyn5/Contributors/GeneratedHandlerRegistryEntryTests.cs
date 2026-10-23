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
    public void EntryIsPublicWhenTheRegistryIsInternal()
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
                     internal abstract class OrdersContributor : IHandlerRegistryContributor
                     {
                         public static void AddHandlers(EndpointConfiguration configuration) { }
                         public static void AddSagas(EndpointConfiguration configuration) { }
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
                     internal abstract class OrdersContributor : IHandlerRegistryContributor
                     {
                         public static void AddHandlers(EndpointConfiguration configuration) { }
                         public static void AddSagas(EndpointConfiguration configuration) { }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void EntryHasAStableHintNameAndNameFromTheAssembly()
    {
        var output = GeneratedCompilation.Run("My.Orders-Assembly", ContributorOnlySource, [new AddHandlerAndSagasRegistrationGenerator()]);

        Assert.That(output.Sources.Select(s => s.HintName), Is.EqualTo(["HandlerRegistrations.g.cs", "HandlerRegistryEntry.g.cs"]));
        Assert.That(output.SourceOf("HandlerRegistryEntry.g.cs"), Does.Contain("public static class MyOrdersAssemblyHandlerRegistryEntry"));
    }

    [Test]
    public void EntryForwardsToTheRootRegistryEvenWhenItIsInternal()
    {
        var output = GeneratedCompilation.Run(nameof(EntryForwardsToTheRootRegistryEvenWhenItIsInternal), InternalRegistrySource, [new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()]);
        Assert.That(output.CompilationErrors, Is.Empty);

        var assembly = GeneratedCompilation.Load(output.Compilation);
        var attribute = assembly.GetCustomAttribute<GeneratedHandlerRegistryAttribute>();

        Assert.That(attribute, Is.Not.Null);
        var entryType = attribute.EntryType;
        Assert.That(entryType.IsPublic && entryType.IsAbstract && entryType.IsSealed, Is.True, "The entry must be a public static class.");
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
                                         internal abstract class OrdersContributor : IHandlerRegistryContributor
                                         {
                                             public static void AddHandlers(EndpointConfiguration configuration) { }
                                             public static void AddSagas(EndpointConfiguration configuration) { }
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
                                          internal abstract class OrdersContributor : IHandlerRegistryContributor
                                          {
                                              public static void AddHandlers(EndpointConfiguration configuration) => Calls.Recorded.Add("AddHandlers");
                                              public static void AddSagas(EndpointConfiguration configuration) => Calls.Recorded.Add("AddSagas");
                                          }
                                          """;
}
