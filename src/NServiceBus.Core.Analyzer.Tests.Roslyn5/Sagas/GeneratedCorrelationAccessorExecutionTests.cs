namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
    public void Interface_property_mapped_on_saga_data_resolves_to_the_implementing_property()
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
                         string CorrelationId { get; }
                     }

                     [Saga]
                     public class GetOnlySaga : Saga<GetOnlySagaData>, IAmStartedByMessages<StartGetOnly>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<GetOnlySagaData> mapper) =>
                             mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<StartGetOnly>(m => m.CorrelationId);

                         public Task Handle(StartGetOnly message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class GetOnlySagaData : ContainSagaData, IHasCorrelationId
                     {
                         public string CorrelationId { get; set; }
                     }

                     public class StartGetOnly : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "GetOnlySagaData");
    }

    [Test]
    public void Init_only_interface_property_on_saga_data_is_written_through_an_extern_accessor()
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
                         string CorrelationId { get; init; }
                     }

                     [Saga]
                     public class InitOnlySaga : Saga<InitOnlySagaData>, IAmStartedByMessages<StartInitOnly>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<InitOnlySagaData> mapper) =>
                             mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<StartInitOnly>(m => m.CorrelationId);

                         public Task Handle(StartInitOnly message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class InitOnlySagaData : ContainSagaData, IHasCorrelationId
                     {
                         public string CorrelationId { get; init; }
                     }

                     public class StartInitOnly : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "InitOnlySagaData");
    }

    [Test]
    public void Get_only_interface_property_on_saga_data_with_a_private_concrete_setter_is_written_through_an_extern_accessor()
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
                         string CorrelationId { get; }
                     }

                     [Saga]
                     public class GetOnlySaga : Saga<GetOnlySagaData>, IAmStartedByMessages<StartGetOnly>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<GetOnlySagaData> mapper) =>
                             mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<StartGetOnly>(m => m.CorrelationId);

                         public Task Handle(StartGetOnly message, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class GetOnlySagaData : ContainSagaData, IHasCorrelationId
                     {
                         public string CorrelationId { get; private set; }
                     }

                     public class StartGetOnly : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "GetOnlySagaData");
    }

    [TestCase("public interface IStart : IEvent { string Id { get; } } public class Impl : IStart { public string Id => \"correlation-value\"; }", "IStart")]
    [TestCase("public interface IBase { string Id { get; } } public interface IStart : IEvent, IBase { } public class Impl : IStart { public string Id => \"correlation-value\"; }", "IStart")]
    [TestCase("public class BaseMessage : ICommand { public string Id => \"correlation-value\"; } public class Impl : BaseMessage { }", "Impl")]
    [TestCase("public class BaseMessage : ICommand { public string Id { internal get; set; } = \"correlation-value\"; } public class Impl : BaseMessage { }", "Impl")]
    public void Message_properties_declared_on_interfaces_and_base_types_are_read(string declarations, string mappedType)
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

                       {{declarations}}

                       [Saga]
                       public class MessageSaga : Saga<MessageSagaData>, IAmStartedByMessages<{{mappedType}}>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MessageSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<{{mappedType}}>(m => m.Id);

                           public Task Handle({{mappedType}} message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class MessageSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        var accessor = GetAccessor<MessagePropertyAccessor>(assembly);
        var message = Activator.CreateInstance(assembly.GetType("Impl")!)!;

        Assert.That(accessor.AccessFrom(message), Is.EqualTo("correlation-value"));
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

    const string AddAllPreamble = """
                                  using System.Threading.Tasks;
                                  using NServiceBus;

                                  public class Test
                                  {
                                      public void Configure(EndpointConfiguration cfg)
                                      {
                                          cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                                      }
                                  }
                                  """;

    [TestCase(true)]
    [TestCase(false)]
    public void Sagas_mapping_an_explicit_implementation_and_a_public_property_each_read_their_own_member(bool explicitFirst)
    {
        var (firstMapping, secondMapping) = explicitFirst ? ("((IHasId)m).Id", "m.Id") : ("m.Id", "((IHasId)m).Id");
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasId
                       {
                           string Id { get; }
                       }

                       public class Start : ICommand, IHasId
                       {
                           string IHasId.Id => "explicit-value";
                           public string Id => "public-value";
                       }

                       [Saga]
                       public class FirstSaga : Saga<FirstSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FirstSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => {{firstMapping}});

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class FirstSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }

                       [Saga]
                       public class SecondSaga : Saga<SecondSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SecondSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => {{secondMapping}});

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class SecondSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        var message = Activator.CreateInstance(assembly.GetType("Start")!)!;
        var values = GetAccessors<MessagePropertyAccessor>(assembly).Select(accessor => accessor.AccessFrom(message));

        Assert.That(values, Is.EquivalentTo((string[])["explicit-value", "public-value"]));
    }

    [Test]
    public void Sagas_mapping_same_named_explicit_properties_of_different_interfaces_each_read_their_own_member()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IFirst
                       {
                           string Id { get; }
                       }

                       public interface ISecond
                       {
                           string Id { get; }
                       }

                       public class Start : ICommand, IFirst, ISecond
                       {
                           string IFirst.Id => "first-value";
                           string ISecond.Id => "second-value";
                       }

                       [Saga]
                       public class FirstSaga : Saga<FirstSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FirstSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((IFirst)m).Id);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class FirstSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }

                       [Saga]
                       public class SecondSaga : Saga<SecondSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SecondSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((ISecond)m).Id);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class SecondSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        var message = Activator.CreateInstance(assembly.GetType("Start")!)!;
        var values = GetAccessors<MessagePropertyAccessor>(assembly).Select(accessor => accessor.AccessFrom(message));

        Assert.That(values, Is.EquivalentTo((string[])["first-value", "second-value"]));
    }

    [TestCase("public", false)]
    [TestCase("public", true)]
    [TestCase("private", false)]
    [TestCase("private", true)]
    public void Message_property_explicitly_implemented_is_read_whether_or_not_the_interface_is_accessible(string interfaceAccessibility, bool implementedOnBaseType)
    {
        const string explicitImplementation = "string IHasId.Id => \"explicit-value\";";
        var (baseInterface, baseImplementation) = implementedOnBaseType ? (" : IHasId", explicitImplementation) : ("", "");
        var (startInterface, startImplementation) = implementedOnBaseType ? ("", "") : (", IHasId", explicitImplementation);
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           {{interfaceAccessibility}} interface IHasId
                           {
                               string Id { get; }
                           }

                           public class BaseMessage{{baseInterface}}
                           {
                               {{baseImplementation}}
                           }

                           public class Start : BaseMessage, ICommand{{startInterface}}
                           {
                               {{startImplementation}}
                           }

                           [Saga]
                           public class ExplicitSaga : Saga<ExplicitSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ExplicitSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((IHasId)m).Id);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class ExplicitSagaData : ContainSagaData
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source, dropMessageHierarchies: interfaceAccessibility == "private");

        var accessor = GetAccessor<MessagePropertyAccessor>(assembly);
        var message = Activator.CreateInstance(assembly.GetType("Outer+Start")!)!;

        Assert.That(accessor.AccessFrom(message), Is.EqualTo("explicit-value"));
        Assert.That(GeneratedSource(source), interfaceAccessibility == "public" ? Does.Not.Contain("extern") : Does.Contain("extern"));
    }

    [Test]
    public void Message_property_implicitly_implementing_a_private_nested_interface_is_read_on_the_concrete_type()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           interface IPrivate
                           {
                               string Prop { get; }
                           }

                           public class Start : ICommand, IPrivate
                           {
                               public string Prop => "implicit-value";
                           }

                           [Saga]
                           public class PrivateSaga : Saga<PrivateSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PrivateSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((IPrivate)m).Prop);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class PrivateSagaData : ContainSagaData
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source, dropMessageHierarchies: true);

        var accessor = GetAccessor<MessagePropertyAccessor>(assembly);
        var message = Activator.CreateInstance(assembly.GetType("Outer+Start")!)!;

        Assert.That(accessor.AccessFrom(message), Is.EqualTo("implicit-value"));
        Assert.That(GeneratedSource(source), Does.Not.Contain("extern"));
    }

    [Test]
    public void Correlation_property_implicitly_implementing_a_private_nested_interface_round_trips_on_the_concrete_type()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           interface IPrivate
                           {
                               string CorrelationId { get; set; }
                           }

                           public class PrivateSagaData : ContainSagaData, IPrivate
                           {
                               public string CorrelationId { get; set; }
                           }

                           [Saga]
                           public class PrivateSaga : Saga<PrivateSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PrivateSagaData> mapper) =>
                                   mapper.MapSaga(s => ((IPrivate)s).CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class Start : ICommand
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "Outer+PrivateSagaData");
        Assert.That(GeneratedSource(source), Does.Not.Contain("extern"));
    }

    [TestCase("public")]
    [TestCase("private")]
    public void Sagas_mapping_an_interface_implementation_hidden_by_a_derived_message_property_each_read_their_own_member(string interfaceAccessibility)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           {{interfaceAccessibility}} interface IHasId
                           {
                               string Id { get; }
                           }

                           public class BaseMessage : IHasId
                           {
                               public string Id => "interface-value";
                           }

                           public class Start : BaseMessage, ICommand
                           {
                               public new string Id => "hidden-value";
                           }

                           [Saga]
                           public class InterfaceSaga : Saga<InterfaceSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<InterfaceSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((IHasId)m).Id);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class InterfaceSagaData : ContainSagaData
                           {
                               public string CorrelationId { get; set; }
                           }

                           [Saga]
                           public class DerivedSaga : Saga<DerivedSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DerivedSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => m.Id);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class DerivedSagaData : ContainSagaData
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source, dropMessageHierarchies: interfaceAccessibility == "private");

        var message = Activator.CreateInstance(assembly.GetType("Outer+Start")!)!;
        var values = GetAccessors<MessagePropertyAccessor>(assembly).Select(accessor => accessor.AccessFrom(message));

        Assert.That(values, Is.EquivalentTo((string[])["interface-value", "hidden-value"]));
    }

    [Test]
    public void Sagas_sharing_saga_data_map_an_interface_implementation_and_the_property_hiding_it_to_their_own_member()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasCorrelationId
                       {
                           string CorrelationId { get; set; }
                       }

                       public class BaseSagaData : ContainSagaData, IHasCorrelationId
                       {
                           public string CorrelationId { get; set; }
                       }

                       public class SharedSagaData : BaseSagaData
                       {
                           public new string CorrelationId { get; set; }
                       }

                       [Saga]
                       public class InterfaceSaga : Saga<SharedSagaData>, IAmStartedByMessages<StartInterface>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SharedSagaData> mapper) =>
                               mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<StartInterface>(m => m.CorrelationId);

                           public Task Handle(StartInterface message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       [Saga]
                       public class DerivedSaga : Saga<SharedSagaData>, IAmStartedByMessages<StartDerived>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SharedSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<StartDerived>(m => m.CorrelationId);

                           public Task Handle(StartDerived message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class StartInterface : ICommand
                       {
                           public string CorrelationId { get; set; }
                       }

                       public class StartDerived : ICommand
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        var baseProperty = assembly.GetType("BaseSagaData")!.GetProperty("CorrelationId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        var derivedProperty = assembly.GetType("SharedSagaData")!.GetProperty("CorrelationId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

        var writtenMembers = GetAccessors<CorrelationPropertyAccessor>(assembly).Select(accessor =>
        {
            var sagaData = (IContainSagaData)Activator.CreateInstance(assembly.GetType("SharedSagaData")!)!;
            accessor.WriteTo(sagaData, "correlation-value");
            return (baseProperty.GetValue(sagaData) as string, derivedProperty.GetValue(sagaData) as string);
        });

        Assert.That(writtenMembers, Is.EquivalentTo((ValueTuple<string, string>[])[("correlation-value", null), (null, "correlation-value")]));
    }

    static string GeneratedSource(string source) =>
        string.Join(Environment.NewLine, RunGenerators(source, new CSharpParseOptions(LanguageVersion.Preview)).SyntaxTrees.Select(t => t.ToString()));

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

    static Assembly CompileAndLoad(string source, bool dropMessageHierarchies = false)
    {
        var outputCompilation = RunGenerators(source, new CSharpParseOptions(LanguageVersion.Preview));

        if (dropMessageHierarchies)
        {
            // The registration code lists every interface of a message, which cannot compile for an inaccessible one; only the accessors are under test.
            var registrationTree = outputCompilation.SyntaxTrees.Single(t => t.ToString().Contains("RegisterMessageTypeWithHierarchy"));
            var withoutHierarchies = Regex.Replace(registrationTree.ToString(), @"(RegisterMessageTypeWithHierarchy\(typeof\([^)]*\)), \[[^\]]*\]\)", "$1, [])");
            outputCompilation = outputCompilation.ReplaceSyntaxTree(registrationTree, CSharpSyntaxTree.ParseText(withoutHierarchies, (CSharpParseOptions)registrationTree.Options, registrationTree.FilePath));
        }

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
