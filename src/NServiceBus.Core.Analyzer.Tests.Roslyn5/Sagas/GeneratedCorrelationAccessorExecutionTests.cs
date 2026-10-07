namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        var (firstValue, secondValue) = explicitFirst ? ("explicit-value", "public-value") : ("public-value", "explicit-value");

        Assert.That(ReadWithGeneratedAccessor(assembly, "FirstSaga", "Start", message), Is.EqualTo(firstValue));
        Assert.That(ReadWithGeneratedAccessor(assembly, "SecondSaga", "Start", message), Is.EqualTo(secondValue));
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

        Assert.That(ReadWithGeneratedAccessor(assembly, "FirstSaga", "Start", message), Is.EqualTo("first-value"));
        Assert.That(ReadWithGeneratedAccessor(assembly, "SecondSaga", "Start", message), Is.EqualTo("second-value"));
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

        Assert.That(ReadWithGeneratedAccessor(assembly, "Outer+InterfaceSaga", "Outer+Start", message), Is.EqualTo("interface-value"));
        Assert.That(ReadWithGeneratedAccessor(assembly, "Outer+DerivedSaga", "Outer+Start", message), Is.EqualTo("hidden-value"));
    }

    [TestCase("string", "\"hidden-value\"", "hidden-value")]
    [TestCase("int", "42", 42)]
    public void Sagas_mapping_a_base_class_property_hidden_by_a_derived_message_property_each_read_their_own_member(string hiddenType, string hiddenValue, object expectedHiddenValue)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class BaseMessage
                       {
                           public string Id => "base-value";
                       }

                       public class Start : BaseMessage, ICommand
                       {
                           public new {{hiddenType}} Id => {{hiddenValue}};
                       }

                       [Saga]
                       public class BaseSaga : Saga<BaseSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<BaseSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((BaseMessage)m).Id);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class BaseSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; } = "";
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
                           public {{hiddenType}} CorrelationId { get; set; } = default!;
                       }
                       """;

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        var message = Activator.CreateInstance(assembly.GetType("Start")!)!;

        Assert.That(ReadWithGeneratedAccessor(assembly, "BaseSaga", "Start", message), Is.EqualTo("base-value"));
        Assert.That(ReadWithGeneratedAccessor(assembly, "DerivedSaga", "Start", message), Is.EqualTo(expectedHiddenValue));
        Assert.That(GeneratedSource(source), Does.Contain("=> ((global::BaseMessage)message).Id;"));
    }

    [Test]
    public void Message_property_cast_to_a_base_class_with_a_getter_only_reachable_from_a_nested_saga_is_read_through_an_extern_accessor()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class BaseMessage
                       {
                           string Id { get; } = "base-value";

                           [Saga]
                           public class NestedSaga : Saga<NestedSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NestedSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((BaseMessage)m).Id);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }
                       }

                       public class Start : BaseMessage, ICommand
                       {
                           public int Id => 42;
                       }

                       public class NestedSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; } = "";
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
                           public int CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        var message = Activator.CreateInstance(assembly.GetType("Start")!)!;

        Assert.That(ReadWithGeneratedAccessor(assembly, "BaseMessage+NestedSaga", "Start", message), Is.EqualTo("base-value"));
        Assert.That(ReadWithGeneratedAccessor(assembly, "DerivedSaga", "Start", message), Is.EqualTo(42));
        Assert.That(GeneratedSource(source), Does.Contain("static extern string AccessFrom_Property(global::BaseMessage message);"));
    }

    [Test]
    public void Message_property_cast_to_a_base_class_whose_property_the_message_overrides_reads_the_override()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class BaseMessage
                       {
                           public virtual string Id => "base-value";
                       }

                       public class Start : BaseMessage, ICommand
                       {
                           public override string Id => "override-value";
                       }

                       [Saga]
                       public class OverrideSaga : Saga<OverrideSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OverrideSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((BaseMessage)m).Id);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class OverrideSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; } = "";
                       }
                       """;

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(ReadWithGeneratedAccessor(assembly, "OverrideSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("override-value"));
        Assert.That(GeneratedSource(source), Does.Contain("=> message.Id;"));
    }

    [TestCase("string IHasId.Id => \"derived-value\";", false)]
    [TestCase("public new string Id => \"derived-value\";", false)]
    [TestCase("string IHasId.Id => \"derived-value\";", true)]
    [TestCase("public new string Id => \"derived-value\";", true)]
    public void Derived_message_re_implementing_the_interface_is_read_through_the_interface(string derivedImplementation, bool obsoleteError)
    {
        var (interfaceAttribute, sagaAttribute) = obsoleteError ? ("[System.Obsolete(\"Use something else\", true)]", "[System.Obsolete]") : ("", "");
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasId
                       {
                           {{interfaceAttribute}} string Id { get; }
                       }

                       public class Start : ICommand, IHasId
                       {
                           public string Id => "start-value";
                       }

                       public class DerivedStart : Start, IHasId
                       {
                           {{derivedImplementation}}
                       }

                       [Saga]
                       {{sagaAttribute}}
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
                       """;

        var assembly = CompileAndLoad(source);

        var derived = Activator.CreateInstance(assembly.GetType("DerivedStart")!)!;
        var mappedValue = assembly.GetType("IHasId")!.GetProperty("Id")!.GetValue(derived);
        var accessor = RegisteredMessageAccessor(assembly, "InterfaceSaga", "Start");

        Assert.That(mappedValue, Is.EqualTo("derived-value"));
        Assert.That(accessor.AccessFrom(derived), Is.EqualTo(mappedValue));
        Assert.That(accessor.AccessFrom(Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("start-value"));
        Assert.That(accessor.GetType().Assembly, Is.SameAs(obsoleteError ? typeof(MessagePropertyAccessor).Assembly : assembly));
    }

    [TestCase("public interface IOther { string Id { get; } } public interface IStart : IEvent, IBase, IOther { } public class Impl : IStart { string IBase.Id => \"base-value\"; string IOther.Id => \"other-value\"; }")]
    [TestCase("public interface IStart : IEvent, IBase { new string Id { get; } } public class Impl : IStart { string IBase.Id => \"base-value\"; public string Id => \"start-value\"; }")]
    public void Interface_message_type_mapped_through_a_base_interface_reads_the_base_interface_member(string declarations)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IBase
                       {
                           string Id { get; }
                       }

                       {{declarations}}

                       [Saga]
                       public class InterfaceSaga : Saga<InterfaceSagaData>, IAmStartedByMessages<IStart>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<InterfaceSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<IStart>(m => ((IBase)m).Id);

                           public Task Handle(IStart message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class InterfaceSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        var message = Activator.CreateInstance(assembly.GetType("Impl")!)!;

        Assert.That(ReadWithGeneratedAccessor(assembly, "InterfaceSaga", "IStart", message), Is.EqualTo("base-value"));
    }

    [Test]
    public void Message_property_explicitly_implementing_an_inaccessible_generic_interface_is_read_by_its_metadata_name()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           interface IHasId<T>
                           {
                               T Id { get; }
                           }

                           public class Start : ICommand, IHasId<string>, IHasId<int>
                           {
                               string IHasId<string>.Id => "generic-value";
                               int IHasId<int>.Id => 42;
                           }

                           [Saga]
                           public class GenericSaga : Saga<GenericSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<GenericSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((IHasId<string>)m).Id);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class GenericSagaData : ContainSagaData
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source, dropMessageHierarchies: true);

        var message = Activator.CreateInstance(assembly.GetType("Outer+Start")!)!;

        Assert.That(ReadWithGeneratedAccessor(assembly, "Outer+GenericSaga", "Outer+Start", message), Is.EqualTo("generic-value"));
        Assert.That(GeneratedSource(source), Does.Contain("Name = \"Outer.IHasId<System.String>.get_Id\""));
    }

    [Test]
    public void Message_property_with_a_default_implementation_in_an_accessible_interface_is_read_through_the_interface()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasId
                       {
                           string Id => "default-value";
                       }

                       public class DefaultSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }

                       public class Start : ICommand, IHasId
                       {
                       }

                       [Saga]
                       public class DefaultSaga : Saga<DefaultSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DefaultSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((IHasId)m).Id);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       """;

        var assembly = CompileAndLoad(source);

        Assert.That(ReadWithGeneratedAccessor(assembly, "DefaultSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("default-value"));
    }

    [Test]
    public void Message_property_with_a_default_implementation_in_an_inaccessible_interface_is_read_by_the_runtime_accessor()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           interface IHasId
                           {
                               string Id => "default-value";
                           }

                           public class Start : ICommand, IHasId
                           {
                           }

                           [Saga]
                           public class DefaultSaga : Saga<DefaultSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DefaultSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => ((IHasId)m).Id);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class DefaultSagaData : ContainSagaData
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source, dropMessageHierarchies: true);

        var accessor = RegisteredMessageAccessor(assembly, "Outer+DefaultSaga", "Outer+Start");

        Assert.That(GetAccessors<MessagePropertyAccessor>(assembly), Is.Empty);
        Assert.That(accessor.GetType().Assembly, Is.SameAs(typeof(MessagePropertyAccessor).Assembly));
        Assert.That(accessor.AccessFrom(Activator.CreateInstance(assembly.GetType("Outer+Start")!)!), Is.EqualTo("default-value"));
    }

    [TestCase("((IHasCorrelationId)s).CorrelationId")]
    [TestCase("(s as IHasCorrelationId).CorrelationId")]
    [TestCase("((CastSagaData)s).CorrelationId")]
    [TestCase("s.Child.CorrelationId")]
    public void Saga_data_mapping_that_does_not_access_a_property_on_the_lambda_parameter_gets_no_generated_accessors(string sagaMapping)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasCorrelationId
                       {
                           string CorrelationId { get; set; }
                       }

                       public class Child
                       {
                           public string CorrelationId { get; set; }
                       }

                       public class CastSagaData : ContainSagaData, IHasCorrelationId
                       {
                           string IHasCorrelationId.CorrelationId { get; set; }
                           public string CorrelationId { get; set; }
                           public Child Child { get; set; }
                       }

                       [Saga]
                       public class CastSaga : Saga<CastSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CastSagaData> mapper) =>
                               mapper.MapSaga(s => {{sagaMapping}}).ToMessage<Start>(m => m.CorrelationId);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class Start : ICommand
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        Assert.That(GetAccessors<CorrelationPropertyAccessor>(assembly), Is.Empty);
        Assert.That(GetAccessors<MessagePropertyAccessor>(assembly), Is.Empty);
        Assert.That(GeneratedSource(source), Does.Contain("(associatedMessages, null, propertyAccessors)"));

        var exception = Assert.Throws<TargetInvocationException>(() => RegisteredSagaMetadata(assembly, "CastSaga"));
        Assert.That(exception.InnerException, Is.TypeOf<ArgumentException>().With.Message.Contains("more than a single dot"));
    }

    [TestCase("get; init;")]
    [TestCase("get; private set;")]
    public void Correlation_property_whose_extern_accessor_would_target_a_generic_base_type_is_accessed_by_the_runtime_accessor(string accessors)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class SagaDataBase<T> : ContainSagaData
                       {
                           public string CorrelationId { {{accessors}} }
                       }

                       public class GenericBaseSagaData : SagaDataBase<int>
                       {
                       }

                       [Saga]
                       public class GenericBaseSaga : Saga<GenericBaseSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<GenericBaseSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class Start : ICommand
                       {
                           public string CorrelationId { get; set; } = "correlation-value";
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertNoGeneratedCorrelationAccessor(source, assembly);
        AssertRuntimeCorrelationRoundTrip(assembly, "GenericBaseSaga", assembly.GetType("GenericBaseSagaData")!);
        Assert.That(ReadWithGeneratedAccessor(assembly, "GenericBaseSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [TestCase("get; init;")]
    [TestCase("get; private set;")]
    public void Correlation_property_of_a_closed_generic_saga_data_type_needing_an_extern_accessor_is_accessed_by_the_runtime_accessor(string accessors)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class GenericSagaData<T> : ContainSagaData
                       {
                           public string CorrelationId { {{accessors}} }
                       }

                       [Saga]
                       public class GenericDataSaga : Saga<GenericSagaData<int>>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<GenericSagaData<int>> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class Start : ICommand
                       {
                           public string CorrelationId { get; set; } = "correlation-value";
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertNoGeneratedCorrelationAccessor(source, assembly);
        AssertRuntimeCorrelationRoundTrip(assembly, "GenericDataSaga", assembly.GetType("GenericSagaData`1")!.MakeGenericType(typeof(int)));
        Assert.That(ReadWithGeneratedAccessor(assembly, "GenericDataSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [Test]
    public void Message_property_whose_extern_accessor_would_target_a_generic_base_type_is_read_by_the_runtime_accessor()
    {
        var source = AttributedPropertySaga(
            "public string CorrelationId { get; set; } = \"\";",
            "public class MessageBase<T> : ICommand { [System.Obsolete(\"Use something else\", true)] public string CorrelationId { get; set; } = \"correlation-value\"; } public class Start : MessageBase<int> { }",
            sagaAttribute: "[System.Obsolete]");

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        var accessor = RegisteredMessageAccessor(assembly, "AttributedSaga", assembly.GetType("Start")!);

        Assert.That(GeneratedSource(source), Does.Not.Contain("extern"));
        Assert.That(GetAccessors<MessagePropertyAccessor>(assembly), Is.Empty);
        Assert.That(accessor.GetType().Assembly, Is.SameAs(typeof(MessagePropertyAccessor).Assembly));
        Assert.That(accessor.AccessFrom(Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
    }

    [Test]
    public void Message_property_explicitly_implementing_an_inaccessible_interface_on_a_generic_message_is_read_by_the_runtime_accessor()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           interface IHasId
                           {
                               string Id { get; }
                           }

                           public class Envelope<T> : ICommand, IHasId
                           {
                               string IHasId.Id => "correlation-value";
                           }

                           [Saga]
                           public class EnvelopeSaga : Saga<EnvelopeSagaData>, IAmStartedByMessages<Envelope<int>>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EnvelopeSagaData> mapper) =>
                                   mapper.MapSaga(s => s.CorrelationId).ToMessage<Envelope<int>>(m => ((IHasId)m).Id);

                               public Task Handle(Envelope<int> message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class EnvelopeSagaData : ContainSagaData
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source, dropMessageHierarchies: true);

        var messageType = assembly.GetType("Outer+Envelope`1")!.MakeGenericType(typeof(int));
        var accessor = RegisteredMessageAccessor(assembly, "Outer+EnvelopeSaga", messageType);

        Assert.That(GeneratedSource(source), Does.Not.Contain("extern"));
        Assert.That(GetAccessors<MessagePropertyAccessor>(assembly), Is.Empty);
        Assert.That(accessor.GetType().Assembly, Is.SameAs(typeof(MessagePropertyAccessor).Assembly));
        Assert.That(accessor.AccessFrom(Activator.CreateInstance(messageType)!), Is.EqualTo("correlation-value"));
    }

    [Test]
    public void Message_property_of_a_generic_message_read_directly_keeps_its_generated_accessor()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Order
                       {
                       }

                       public class Envelope<T> : ICommand
                       {
                           public string Id { get; set; } = "correlation-value";
                       }

                       [Saga]
                       public class EnvelopeSaga : Saga<EnvelopeSagaData>, IAmStartedByMessages<Envelope<Order>>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EnvelopeSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<Envelope<Order>>(m => m.Id);

                           public Task Handle(Envelope<Order> message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class EnvelopeSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        var messageType = assembly.GetType("Envelope`1")!.MakeGenericType(assembly.GetType("Order")!);
        var accessor = RegisteredMessageAccessor(assembly, "EnvelopeSaga", messageType);

        Assert.That(GeneratedSource(source), Does.Contain("protected override object? AccessFrom(global::Envelope<global::Order> message) => message.Id;"));
        Assert.That(accessor.GetType().Assembly, Is.SameAs(assembly));
        Assert.That(accessor.AccessFrom(Activator.CreateInstance(messageType)!), Is.EqualTo("correlation-value"));
    }

    const string LegacyObsolete = "[System.Obsolete(\"Use something else\", DiagnosticId = \"LEGACY001\")]";
    const string Experimental = "[System.Diagnostics.CodeAnalysis.Experimental(\"EXP001\")]";

    const string MessageAccessFrom = "protected override object? AccessFrom(global::Start message)";
    const string CorrelationAccessFrom = "public override object? AccessFrom(NServiceBus.IContainSagaData sagaData)";
    const string CorrelationWriteTo = "public override void WriteTo(NServiceBus.IContainSagaData sagaData, object value)";

    static string AttributedPropertySaga(string sagaDataProperty, string messageDeclarations, string messageMapping = "m.CorrelationId", string mappingSuppression = null, string sagaAttribute = "") =>
        $$"""
          {{AddAllPreamble}}

          {{messageDeclarations}}

          [Saga]
          {{sagaAttribute}}
          public class AttributedSaga : Saga<AttributedSagaData>, IAmStartedByMessages<Start>
          {
              protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AttributedSagaData> mapper)
              {
          {{(mappingSuppression is null ? "" : $"#pragma warning disable {mappingSuppression}")}}
                  mapper.MapSaga(s => s.CorrelationId).ToMessage<Start>(m => {{messageMapping}});
          {{(mappingSuppression is null ? "" : $"#pragma warning restore {mappingSuppression}")}}
              }

              public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
          }

          public class AttributedSagaData : ContainSagaData
          {
              {{sagaDataProperty}}
          }
          """;

    [TestCase(LegacyObsolete, "LEGACY001")]
    [TestCase(Experimental, "EXP001")]
    public void Generated_accessors_suppress_custom_diagnostics_of_locally_suppressed_mappings(string attribute, string diagnosticId)
    {
        var source = AttributedPropertySaga(
            $"{attribute} public string CorrelationId {{ get; set; }} = \"\";",
            $"public class Start : ICommand {{ {attribute} public string CorrelationId {{ get; set; }} = \"correlation-value\"; }}",
            mappingSuppression: diagnosticId);

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(MembersSuppressing(GeneratedSource(source), diagnosticId), Is.EquivalentTo(new[] { MessageAccessFrom, CorrelationAccessFrom, CorrelationWriteTo }));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
        Assert.That(ReadWithGeneratedAccessor(assembly, "AttributedSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [TestCase(LegacyObsolete, "{0} get; set;", "LEGACY001", new[] { MessageAccessFrom, CorrelationAccessFrom })]
    [TestCase(LegacyObsolete, "get; {0} set;", null, new[] { CorrelationWriteTo })]
    [TestCase(Experimental, "{0} get; set;", "EXP001", new[] { MessageAccessFrom, CorrelationAccessFrom })]
    [TestCase(Experimental, "get; {0} set;", null, new[] { CorrelationWriteTo })]
    public void Generated_accessors_suppress_accessor_diagnostics_only_around_the_member_that_calls_the_accessor(string attribute, string accessorsFormat, string mappingSuppression, string[] suppressedMembers)
    {
        var accessors = string.Format(accessorsFormat, attribute);
        var diagnosticId = attribute == Experimental ? "EXP001" : "LEGACY001";
        var source = AttributedPropertySaga(
            $"public string CorrelationId {{ {accessors} }} = \"\";",
            $"public class Start : ICommand {{ public string CorrelationId {{ {accessors} }} = \"correlation-value\"; }}",
            mappingSuppression: mappingSuppression);

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(MembersSuppressing(GeneratedSource(source), diagnosticId), Is.EquivalentTo(suppressedMembers));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
        Assert.That(ReadWithGeneratedAccessor(assembly, "AttributedSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [Test]
    public void Generated_accessors_suppress_all_custom_diagnostics_of_a_property_in_one_pragma()
    {
        var attributes = $"{Experimental}{LegacyObsolete}";
        var source = AttributedPropertySaga(
            $"{attributes} public string CorrelationId {{ get; set; }} = \"\";",
            $"public class Start : ICommand {{ {attributes} public string CorrelationId {{ get; set; }} = \"correlation-value\"; }}",
            mappingSuppression: "LEGACY001, EXP001");

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(MembersSuppressing(GeneratedSource(source), "EXP001, LEGACY001"), Is.EquivalentTo(new[] { MessageAccessFrom, CorrelationAccessFrom, CorrelationWriteTo }));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
    }

    [TestCase("[System.Diagnostics.CodeAnalysis.Experimental(\"EXP001\")] public virtual string CorrelationId { get; set; } = \"\";", "public override string CorrelationId { get; set; } = \"\";", "EXP001", true)]
    [TestCase("public virtual string CorrelationId { get; set; } = \"\";", "[System.Diagnostics.CodeAnalysis.Experimental(\"EXP001\")] public override string CorrelationId { get; set; } = \"\";", null, false)]
    public void Generated_accessors_suppress_the_diagnostics_of_the_overridden_property(string baseProperty, string overridingProperty, string mappingSuppression, bool suppressed)
    {
        var source = AttributedPropertySaga(
            overridingProperty,
            "public class Start : ICommand { public string CorrelationId { get; set; } = \"correlation-value\"; }",
            mappingSuppression: mappingSuppression)
            .Replace("public class AttributedSagaData : ContainSagaData", $"public class AttributedSagaBase : ContainSagaData {{ {baseProperty} }} public class AttributedSagaData : AttributedSagaBase");

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(MembersSuppressing(GeneratedSource(source), "EXP001"), Is.EquivalentTo(suppressed ? new[] { CorrelationAccessFrom, CorrelationWriteTo } : []));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
    }

    [TestCase("CS0618", "[System.Obsolete(\"Use something else\")]")]
    [TestCase("CS0612", "[System.Obsolete]")]
    public void Generated_accessors_leave_diagnostic_ids_suppressed_file_wide_out_of_their_pragmas(string customDiagnosticId, string ordinaryObsolete)
    {
        var source = AttributedPropertySaga(
            $"{ordinaryObsolete} public string CorrelationId {{ get; set; }} = \"\";",
            $"public class Start : ICommand {{ [System.Obsolete(\"Use something else\", DiagnosticId = \"{customDiagnosticId}\")] public string CorrelationId {{ get; set; }} = \"correlation-value\"; }}",
            sagaAttribute: "[System.Obsolete]");

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(GeneratedSource(source), Does.Not.Contain($"#pragma warning restore {customDiagnosticId}"));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
        Assert.That(ReadWithGeneratedAccessor(assembly, "AttributedSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [Test]
    public void Setter_that_reports_an_obsolete_error_is_written_through_an_extern_accessor()
    {
        var source = AttributedPropertySaga(
            "public string CorrelationId { get; [System.Obsolete(\"Use something else\", true)] set; } = \"\";",
            "public class Start : ICommand { public string CorrelationId { get; set; } = \"correlation-value\"; }");

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        var generated = GeneratedSource(source);
        Assert.That(generated, Does.Contain("WriteTo_Property((global::AttributedSagaData)sagaData, (string)value)"));
        Assert.That(generated, Does.Contain("((global::AttributedSagaData)sagaData).CorrelationId;"));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
    }

    // An obsolete context is the only place a mapping can read an obsolete error, and the only suppression a pragma can't name.
    [TestCase("[System.Obsolete(\"Use something else\", true)]")]
    [TestCase("[System.Obsolete(\"Use something else\", true, DiagnosticId = \"LEGACY001\")]")]
    [TestCase("[System.Obsolete(\"Use something else\", DiagnosticId = \"LEGACY-001\")]")]
    [TestCase("[System.Obsolete(\"Use something else\", DiagnosticId = \"true\")]")]
    public void Properties_with_diagnostics_a_pragma_cannot_suppress_are_accessed_through_extern_accessors(string attribute)
    {
        var source = AttributedPropertySaga(
            $"{attribute} public string CorrelationId {{ get; set; }} = \"\";",
            $"public class Start : ICommand {{ {attribute} public string CorrelationId {{ get; set; }} = \"correlation-value\"; }}",
            sagaAttribute: "[System.Obsolete]");

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        var generated = GeneratedSource(source);
        Assert.That(generated, Does.Contain("=> AccessFrom_Property(message);"));
        Assert.That(generated, Does.Contain("=> AccessFrom_Property((global::AttributedSagaData)sagaData);"));
        Assert.That(generated, Does.Contain("=> WriteTo_Property((global::AttributedSagaData)sagaData, (string)value);"));
        Assert.That(generated, Does.Not.Contain("#pragma warning disable LEGACY"));
        AssertCorrelationRoundTrip(assembly, "AttributedSagaData");
        Assert.That(ReadWithGeneratedAccessor(assembly, "AttributedSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [TestCase("public interface IHasId { [System.Obsolete(\"Use something else\", DiagnosticId = \"LEGACY001\")] string Id { get; } }", "public string Id => \"correlation-value\";", "LEGACY001", true)]
    [TestCase("public interface IHasId { string Id { [System.Diagnostics.CodeAnalysis.Experimental(\"LEGACY001\")] get; } }", "public string Id => \"correlation-value\";", "LEGACY001", true)]
    [TestCase("public interface IHasId { string Id { get; } }", "[System.Obsolete(\"Use something else\", DiagnosticId = \"LEGACY001\")] public string Id => \"correlation-value\";", null, false)]
    public void Message_property_mapped_through_an_interface_suppresses_the_diagnostics_of_the_interface_member(string interfaceDeclaration, string implementation, string mappingSuppression, bool suppressed)
    {
        var source = AttributedPropertySaga(
            "public string CorrelationId { get; set; } = \"\";",
            $"{interfaceDeclaration} public class Start : ICommand, IHasId {{ {implementation} }}",
            "((IHasId)m).Id",
            mappingSuppression);

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        var generated = GeneratedSource(source);
        Assert.That(generated, Does.Contain("=> ((global::IHasId)message).Id;"));
        Assert.That(MembersSuppressing(generated, "LEGACY001"), Is.EquivalentTo(suppressed ? new[] { MessageAccessFrom } : []));
        Assert.That(ReadWithGeneratedAccessor(assembly, "AttributedSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [TestCase("public string Id => \"correlation-value\";", "=> message.Id;")]
    [TestCase("string IHasId.Id => \"correlation-value\";", "=> AccessFrom_Property(message);")]
    public void Message_property_mapped_through_an_interface_member_with_an_obsolete_error_is_read_through_the_implementation_of_a_sealed_message(string implementation, string expectedRead)
    {
        var source = AttributedPropertySaga(
            "public string CorrelationId { get; set; } = \"\";",
            $"public interface IHasId {{ [System.Obsolete(\"Use something else\", true)] string Id {{ get; }} }} public sealed class Start : ICommand, IHasId {{ {implementation} }}",
            "((IHasId)m).Id",
            sagaAttribute: "[System.Obsolete]");

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(GeneratedSource(source), Does.Contain(expectedRead));
        Assert.That(ReadWithGeneratedAccessor(assembly, "AttributedSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    [Test]
    public void Interface_message_cast_to_a_class_whose_property_reports_an_obsolete_error_is_read_through_an_extern_accessor_on_the_class()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IStart : ICommand { }

                       public class Start : IStart
                       {
                           [System.Obsolete("Use something else", true)]
                           public string Id { get; set; } = "correlation-value";
                       }

                       [Saga]
                       [System.Obsolete]
                       public class CastSaga : Saga<CastSagaData>, IAmStartedByMessages<IStart>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CastSagaData> mapper) =>
                               mapper.MapSaga(s => s.CorrelationId).ToMessage<IStart>(m => ((Start)m).Id);

                           public Task Handle(IStart message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class CastSagaData : ContainSagaData
                       {
                           public string CorrelationId { get; set; } = "";
                       }
                       """;

        var assembly = CompileAndLoad(source, warningsAsErrors: true);

        Assert.That(GeneratedSource(source), Does.Contain("=> AccessFrom_Property((global::Start)message);"));
        Assert.That(ReadWithGeneratedAccessor(assembly, "CastSaga", "IStart", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("correlation-value"));
    }

    static string[] MembersSuppressing(string generated, string diagnosticIds) =>
    [
        .. Regex.Matches(generated, $@"#pragma warning disable {Regex.Escape(diagnosticIds)}\r?\n\s*(?<member>(protected|public) override [^\r\n]*?) =>[^\r\n]*\r?\n\s*#pragma warning restore {Regex.Escape(diagnosticIds)}\r?\n")
            .Select(match => match.Groups["member"].Value)
    ];

    static string GeneratedSource(string source) =>
        string.Join(Environment.NewLine, RunGenerators(source, new CSharpParseOptions(LanguageVersion.Preview)).SyntaxTrees.Select(t => t.ToString()));

    static T[] GetAccessors<T>(Assembly assembly) =>
    [
        .. assembly.GetTypes()
            .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (T)t.GetField("Instance")!.GetValue(null)!)
    ];

    static T GetAccessor<T>(Assembly assembly) => GetAccessors<T>(assembly).Single();

    static object ReadWithGeneratedAccessor(Assembly assembly, string sagaTypeName, string messageTypeName, object message)
    {
        var accessor = RegisteredMessageAccessor(assembly, sagaTypeName, messageTypeName);
        Assert.That(accessor.GetType().Assembly, Is.SameAs(assembly), $"{sagaTypeName} is not registered with a generated message accessor.");
        return accessor.AccessFrom(message);
    }

    static SagaMetadata RegisteredSagaMetadata(Assembly assembly, string sagaTypeName) =>
        SagaAccessorCompilation.RegisteredSagas(assembly).Find(assembly.GetType(sagaTypeName)!);

    static MessagePropertyAccessor RegisteredMessageAccessor(Assembly assembly, string sagaTypeName, string messageTypeName) =>
        RegisteredMessageAccessor(assembly, sagaTypeName, assembly.GetType(messageTypeName)!);

    static MessagePropertyAccessor RegisteredMessageAccessor(Assembly assembly, string sagaTypeName, Type messageType)
    {
        var metadata = RegisteredSagaMetadata(assembly, sagaTypeName);
        Assert.That(metadata.TryGetFinder(messageType.FullName!, out var finderDefinition), Is.True);
        return SagaAccessorCompilation.MessageAccessor(finderDefinition);
    }

    static void AssertCorrelationRoundTrip(Assembly assembly, string sagaDataTypeName)
    {
        var accessor = GetAccessor<CorrelationPropertyAccessor>(assembly);
        var sagaData = (IContainSagaData)Activator.CreateInstance(assembly.GetType(sagaDataTypeName)!)!;

        accessor.WriteTo(sagaData, "correlation-value");

        Assert.That(accessor.AccessFrom(sagaData), Is.EqualTo("correlation-value"));
    }

    static void AssertNoGeneratedCorrelationAccessor(string source, Assembly assembly)
    {
        var generated = GeneratedSource(source);
        Assert.That(generated, Does.Not.Contain("extern"));
        Assert.That(generated, Does.Contain("(associatedMessages, null, propertyAccessors)"));
        Assert.That(GetAccessors<CorrelationPropertyAccessor>(assembly), Is.Empty);
    }

    static void AssertRuntimeCorrelationRoundTrip(Assembly assembly, string sagaTypeName, Type sagaDataType)
    {
        Assert.That(RegisteredSagaMetadata(assembly, sagaTypeName).TryGetCorrelationProperty(out var correlationProperty), Is.True);
        var accessor = correlationProperty!.Accessor;
        var sagaData = (IContainSagaData)Activator.CreateInstance(sagaDataType)!;

        accessor.WriteTo(sagaData, "correlation-value");

        Assert.That(accessor.GetType().Assembly, Is.SameAs(typeof(CorrelationPropertyAccessor).Assembly));
        Assert.That(accessor.AccessFrom(sagaData), Is.EqualTo("correlation-value"));
        Assert.That(sagaDataType.GetProperty("CorrelationId")!.GetValue(sagaData), Is.EqualTo("correlation-value"));
    }

    static Assembly CompileAndLoad(string source, bool dropMessageHierarchies = false, bool warningsAsErrors = false)
    {
        var outputCompilation = RunGenerators(source, new CSharpParseOptions(LanguageVersion.Preview), warningsAsErrors);

        if (dropMessageHierarchies)
        {
            outputCompilation = SagaAccessorCompilation.WithoutMessageHierarchies(outputCompilation);
        }

        using var peStream = new MemoryStream();
        var emitResult = outputCompilation.Emit(peStream);

        var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));

        return Assembly.Load(peStream.ToArray());
    }

    static Compilation RunGenerators(string source, CSharpParseOptions parseOptions, bool warningsAsErrors = false) =>
        SagaAccessorCompilation.RunGenerators(SagaAccessorCompilation.CreateCompilation(source, parseOptions, warningsAsErrors), parseOptions);
}
