namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Analyzer;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class GeneratedCorrelationAccessorExecutionTests
{
    [Test]
    public void Generated_correlation_accessors_round_trip_for_colliding_saga_data_properties()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     namespace First
                     {
                         [Saga]
                         public class SagaA : Saga<SagaAData>, IAmStartedByMessages<StartMessageA>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaAData> mapper) =>
                                 mapper.MapSaga(s => s.CorrelationId).ToMessage<StartMessageA>(m => m.CorrelationId);

                             public Task Handle(StartMessageA message, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         public class SagaAData : ContainSagaData
                         {
                             public string CorrelationId { get; set; }
                         }

                         public class StartMessageA : ICommand
                         {
                             public string CorrelationId { get; set; }
                         }
                     }

                     namespace Second
                     {
                         [Saga]
                         public class SagaB : Saga<SagaBData>, IAmStartedByMessages<StartMessageB>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaBData> mapper) =>
                                 mapper.MapSaga(s => s.CorrelationId).ToMessage<StartMessageB>(m => m.CorrelationId);

                             public Task Handle(StartMessageB message, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         public class SagaBData : ContainSagaData
                         {
                             public string CorrelationId { get; set; }
                         }

                         public class StartMessageB : ICommand
                         {
                             public string CorrelationId { get; set; }
                         }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        // Two saga-data classes with a colliding property name/type must not share one generated accessor.
        var accessors = GetAccessors<CorrelationPropertyAccessor>(assembly);

        Assert.That(accessors, Has.Length.EqualTo(2), "Each saga-data class must get its own generated correlation accessor.");

        var sagaDataTypes = assembly.GetTypes()
            .Where(t => typeof(IContainSagaData).IsAssignableFrom(t) && !t.IsAbstract)
            .ToArray();

        Assert.That(sagaDataTypes, Has.Length.EqualTo(2));

        var claimedSagaDataTypes = new HashSet<Type>();
        foreach (var accessor in accessors)
        {
            // The generated accessor casts to its concrete saga-data type, so a mismatching instance throws.
            var matching = sagaDataTypes.Where(sagaDataType =>
            {
                var sagaData = (IContainSagaData)Activator.CreateInstance(sagaDataType)!;
                try
                {
                    accessor.WriteTo(sagaData, "correlation-value");
                    return Equals(accessor.AccessFrom(sagaData), "correlation-value");
                }
                catch (InvalidCastException)
                {
                    return false;
                }
            }).ToArray();

            Assert.That(matching, Has.Length.EqualTo(1), $"Accessor {accessor.GetType().Name} must round-trip for exactly one saga-data type.");
            Assert.That(claimedSagaDataTypes.Add(matching[0]), Is.True, $"{matching[0].Name} is served by more than one accessor.");
        }
    }

    const string InitOnlySagaSource = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     [Saga]
                     public class InitSaga : Saga<InitSagaData>, IAmStartedByMessages<StartInit>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<InitSagaData> mapper) =>
                             mapper.MapSaga(s => s.CorrelationId).ToMessage<StartInit>(m => m.CorrelationId);

                         public Task Handle(StartInit message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class InitSagaData : ContainSagaData
                     {
                         public string CorrelationId { get; init; }
                     }

                     public class StartInit : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

    [Test]
    public void Init_only_correlation_property_round_trips_through_a_generated_accessor()
    {
        var assembly = CompileAndLoad(InitOnlySagaSource);

        AssertCorrelationRoundTrip(assembly, "InitSagaData");
    }

    [TestCase(false, "static extern void WriteTo_Property")]
    [TestCase(true, "static safe extern void WriteTo_Property")]
    public void Init_only_extern_accessor_is_marked_safe_only_under_the_updated_memory_safety_rules(bool updatedRules, string expected)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview).WithFeatures(updatedRules ? [new KeyValuePair<string, string>("updated-memory-safety-rules", "true")] : []);

        var generated = string.Join(Environment.NewLine, RunGenerators(InitOnlySagaSource, parseOptions).SyntaxTrees.Select(t => t.ToString()));

        Assert.That(generated, Does.Contain(expected));
        // Everything except an init-only setter is reached directly.
        Assert.That(generated, Does.Not.Contain("AccessFrom_Property"));
    }

    [TestCase("public string CorrelationId { get; private set; }", "")]
    [TestCase("public string CorrelationId { get; protected set; }", "")]
    [TestCase("public string CorrelationId { get; init; }", "")]
    [TestCase("", "public string CorrelationId { get; init; }")]
    [TestCase("", "public string CorrelationId { get; private set; }")]
    public void Setters_that_generated_code_cannot_assign_round_trip_through_an_extern_accessor(string derivedProperty, string baseProperty)
    {
        var source = $$"""
                       using System.Threading.Tasks;
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg)
                           {
                               cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                           }
                       }

                       [Saga]
                       public class ExternSaga : Saga<ExternSagaData>, IAmStartedByMessages<StartExtern>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ExternSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<StartExtern>(m => m.CorrelationId);

                           public Task Handle(StartExtern message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class ExternSagaBase : ContainSagaData { {{baseProperty}} }

                       public class ExternSagaData : ExternSagaBase { {{derivedProperty}} }

                       public class StartExtern : ICommand
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "ExternSagaData");
    }

    [Test]
    public void Setter_declared_in_another_part_of_a_partial_saga_data_class_round_trips()
    {
        var source = $$"""
                       using System.Threading.Tasks;
                       using NServiceBus;

                       public class Test
                       {
                           public void Configure(EndpointConfiguration cfg)
                           {
                               cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                           }
                       }

                       [Saga]
                       public class ExternSaga : Saga<ExternSagaData>, IAmStartedByMessages<StartExtern>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ExternSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<StartExtern>(m => m.CorrelationId);

                           public Task Handle(StartExtern message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class ExternSagaBase : ContainSagaData {  }

                       public partial class ExternSagaData : ExternSagaBase { }

                       public partial class ExternSagaData { public string CorrelationId { get; private set; } }

                       public class StartExtern : ICommand
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "ExternSagaData");
    }

    [Test]
    public void Message_getter_only_reachable_from_a_nested_saga_is_read_through_an_extern_accessor()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     public class OuterMessage : ICommand
                     {
                         public string CorrelationId { private get; set; }

                         [Saga]
                         public class NestedSaga : Saga<NestedSagaData>, IAmStartedByMessages<OuterMessage>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NestedSagaData> mapper) =>
                                 mapper.MapSaga(s => s.CorrelationId).ToMessage<OuterMessage>(m => m.CorrelationId);

                             public Task Handle(OuterMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class NestedSagaData : ContainSagaData
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        var accessor = GetAccessor<MessagePropertyAccessor>(assembly);
        var message = Activator.CreateInstance(assembly.GetType("OuterMessage")!)!;
        message.GetType().GetProperty("CorrelationId")!.SetValue(message, "correlation-value");

        Assert.That(accessor.AccessFrom(message), Is.EqualTo("correlation-value"));
    }

    [Test]
    public void Correlation_getter_only_reachable_from_a_nested_saga_is_read_through_an_extern_accessor()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     public class OuterSagaData : ContainSagaData
                     {
                         public string CorrelationId { private get; set; }

                         [Saga]
                         public class NestedSaga : Saga<OuterSagaData>, IAmStartedByMessages<StartNested>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OuterSagaData> mapper) =>
                                 mapper.MapSaga(s => s.CorrelationId).ToMessage<StartNested>(m => m.CorrelationId);

                             public Task Handle(StartNested message, IMessageHandlerContext context) => Task.CompletedTask;
                         }
                     }

                     public class StartNested : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "OuterSagaData");
    }

    [Test]
    public void Message_property_mapped_through_an_explicit_interface_implementation_is_read()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     public interface IHasId
                     {
                         string Id { get; }
                     }

                     [Saga]
                     public class InterfaceSaga : Saga<InterfaceSagaData>, IAmStartedByMessages<StartInterface>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<InterfaceSagaData> mapper) =>
                             mapper.MapSaga(s => s.Id2).ToMessage<StartInterface>(m => ((IHasId)m).Id);

                         public Task Handle(StartInterface message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class InterfaceSagaData : ContainSagaData
                     {
                         public string Id2 { get; set; }
                     }

                     public class StartInterface : ICommand, IHasId
                     {
                         string IHasId.Id => "correlation-value";
                     }
                     """;

        var assembly = CompileAndLoad(source);

        var accessor = GetAccessor<MessagePropertyAccessor>(assembly);
        var message = Activator.CreateInstance(assembly.GetType("StartInterface")!)!;

        Assert.That(accessor.AccessFrom(message), Is.EqualTo("correlation-value"));
    }

    [Test]
    public void Correlation_property_mapped_through_an_explicit_interface_implementation_round_trips()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     public interface IHasCorrelationId
                     {
                         string CorrelationId { get; set; }
                     }

                     [Saga]
                     public class ExplicitSaga : Saga<ExplicitSagaData>, IAmStartedByMessages<StartExplicit>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ExplicitSagaData> mapper) =>
                             mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<StartExplicit>(m => m.CorrelationId);

                         public Task Handle(StartExplicit message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class ExplicitSagaData : ContainSagaData, IHasCorrelationId
                     {
                         string IHasCorrelationId.CorrelationId { get; set; }
                     }

                     public class StartExplicit : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "ExplicitSagaData");
    }

    [Test]
    public void Properties_named_like_keywords_are_accessed_with_an_escape()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     [Saga]
                     public class KeywordSaga : Saga<KeywordSagaData>, IAmStartedByMessages<StartKeyword>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<KeywordSagaData> mapper) =>
                             mapper.MapSaga(s => s.@event).ToMessage<StartKeyword>(m => m.@event);

                         public Task Handle(StartKeyword message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class KeywordSagaData : ContainSagaData
                     {
                         public string @event { get; set; }
                     }

                     public class StartKeyword : ICommand
                     {
                         public string @event { get; set; }
                     }
                     """;

        Assert.DoesNotThrow(() => CompileAndLoad(source));
    }

    static T[] GetAccessors<T>(Assembly assembly) =>
    [
        .. assembly.GetTypes()
            .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (T)t.GetField("Instance")!.GetValue(null)!)
    ];

    static T GetAccessor<T>(Assembly assembly) => GetAccessors<T>(assembly).Single();

    static void AssertCorrelationRoundTrip(Assembly assembly, string sagaDataTypeName)
    {
        var accessor = GetAccessor<CorrelationPropertyAccessor>(assembly);
        var sagaData = (IContainSagaData)Activator.CreateInstance(assembly.GetType(sagaDataTypeName)!)!;

        accessor.WriteTo(sagaData, "correlation-value");

        Assert.That(accessor.AccessFrom(sagaData), Is.EqualTo("correlation-value"));
    }

    static Assembly CompileAndLoad(string source)
    {
        var outputCompilation = RunGenerators(source, new CSharpParseOptions(LanguageVersion.Preview));

        using var peStream = new MemoryStream();
        var emitResult = outputCompilation.Emit(peStream);

        var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));

        return Assembly.Load(peStream.ToArray());
    }

    static Compilation RunGenerators(string source, CSharpParseOptions parseOptions)
    {
        var sourceTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            "CollidingAccessors",
            [sourceTree],
            ReferenceAssemblyPaths(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            [
                new AddSagaGenerator().AsSourceGenerator(),
                new AddHandlerAndSagasRegistrationGenerator().AsSourceGenerator()
            ],
            parseOptions: parseOptions);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        return outputCompilation;
    }

    static MetadataReference[] ReferenceAssemblyPaths() =>
    [
        .. AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrWhiteSpace(a.Location))
            .Select(MetadataReference (a) => MetadataReference.CreateFromFile(a.Location))
    ];
}
