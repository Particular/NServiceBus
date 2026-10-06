namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Analyzer;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NServiceBus.Configuration.AdvancedExtensibility;
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
    public void Get_only_interface_property_on_saga_data_is_written_through_the_public_implementing_setter()
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
        Assert.That(GeneratedSource(source), Does.Contain("((global::GetOnlySagaData)sagaData).CorrelationId = ").And.Not.Contain("extern"));
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

        Assert.That(ReadWithGeneratedAccessor(assembly, "Outer+InterfaceSaga", "Outer+Start", message), Is.EqualTo("interface-value"));
        Assert.That(ReadWithGeneratedAccessor(assembly, "Outer+DerivedSaga", "Outer+Start", message), Is.EqualTo("hidden-value"));
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

        (string Base, string Derived) WrittenMembers(string sagaTypeName)
        {
            var sagaData = (IContainSagaData)Activator.CreateInstance(assembly.GetType("SharedSagaData")!)!;
            RegisteredCorrelationAccessor(assembly, source, sagaTypeName).WriteTo(sagaData, "correlation-value");
            return (baseProperty.GetValue(sagaData) as string, derivedProperty.GetValue(sagaData) as string);
        }

        Assert.That(GetAccessors<CorrelationPropertyAccessor>(assembly), Has.Length.EqualTo(2));
        Assert.That(WrittenMembers("InterfaceSaga"), Is.EqualTo(("correlation-value", (string)null)));
        Assert.That(WrittenMembers("DerivedSaga"), Is.EqualTo(((string)null, "correlation-value")));
    }

    [TestCase("string IHasId.Id => \"derived-value\";")]
    [TestCase("public new string Id => \"derived-value\";")]
    public void Derived_message_re_implementing_the_interface_is_read_through_the_interface(string derivedImplementation)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasId
                       {
                           string Id { get; }
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

        Assert.That(mappedValue, Is.EqualTo("derived-value"));
        Assert.That(ReadWithGeneratedAccessor(assembly, "InterfaceSaga", "Start", derived), Is.EqualTo(mappedValue));
        Assert.That(ReadWithGeneratedAccessor(assembly, "InterfaceSaga", "Start", Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("start-value"));
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
    public void Saga_data_property_explicitly_implementing_an_inaccessible_generic_interface_round_trips_by_its_metadata_name()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           interface IHasCorrelationId<T>
                           {
                               T CorrelationId { get; set; }
                           }

                           public class GenericSagaData : ContainSagaData, IHasCorrelationId<string>
                           {
                               string IHasCorrelationId<string>.CorrelationId { get; set; }
                               public string CorrelationId { get; set; }
                           }

                           [Saga]
                           public class GenericSaga : Saga<GenericSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<GenericSagaData> mapper) =>
                                   mapper.MapSaga(s => ((IHasCorrelationId<string>)s).CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class Start : ICommand
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertExplicitCorrelationRoundTrip(assembly, "Outer+GenericSagaData", "IHasCorrelationId`1");
        Assert.That(GeneratedSource(source), Does.Contain("Name = \"Outer.IHasCorrelationId<System.String>.get_CorrelationId\"").And.Contain("Name = \"Outer.IHasCorrelationId<System.String>.set_CorrelationId\""));
    }

    [Test]
    public void Settable_interface_property_on_saga_data_is_written_through_the_interface()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasCorrelationId
                       {
                           string CorrelationId { get; set; }
                       }

                       public class ExplicitSagaData : ContainSagaData, IHasCorrelationId
                       {
                           string IHasCorrelationId.CorrelationId { get; set; }
                           public string CorrelationId { get; set; }
                       }

                       [Saga]
                       public class ExplicitSaga : Saga<ExplicitSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ExplicitSagaData> mapper) =>
                               mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class Start : ICommand
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertExplicitCorrelationRoundTrip(assembly, "ExplicitSagaData", "IHasCorrelationId");
        Assert.That(GeneratedSource(source), Does.Contain("((global::IHasCorrelationId)sagaData).CorrelationId = ").And.Not.Contain("extern"));
    }

    [TestCase("public", false)]
    [TestCase("public", true)]
    [TestCase("private", false)]
    public void Saga_data_property_explicitly_implemented_on_a_base_class_round_trips_on_the_explicit_member(string interfaceAccessibility, bool baseInReferencedAssembly)
    {
        // NSB0007 flags the explicit implementation when the base class is in source; a referenced assembly isn't analyzed.
        const string baseDeclarations = """
                                        interface IHasCorrelationId
                                        {
                                            string CorrelationId { get; set; }
                                        }

                                        public class BaseSagaData : NServiceBus.ContainSagaData, IHasCorrelationId
                                        {
                                            string IHasCorrelationId.CorrelationId { get; set; }
                                        }
                                        """;
        var referencedSource = baseInReferencedAssembly ? $"{interfaceAccessibility} {baseDeclarations}" : null;
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           {{(baseInReferencedAssembly ? "" : $"{interfaceAccessibility} {baseDeclarations}")}}

                           public class ExplicitSagaData : BaseSagaData
                           {
                               public string CorrelationId { get; set; }
                           }

                           [Saga]
                           public class ExplicitSaga : Saga<ExplicitSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ExplicitSagaData> mapper) =>
                                   mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class Start : ICommand
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source, referencedSource: referencedSource);

        AssertExplicitCorrelationRoundTrip(assembly, "Outer+ExplicitSagaData", "IHasCorrelationId");
        Assert.That(GeneratedSource(source, referencedSource), interfaceAccessibility == "public" ? Does.Not.Contain("extern") : Does.Contain("Name = \"Outer.IHasCorrelationId.set_CorrelationId\""));
    }

    [TestCase("private set;")]
    [TestCase("init;")]
    public void Get_only_interface_property_on_saga_data_is_read_through_the_interface_and_written_through_the_implementing_setter(string setter)
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasCorrelationId
                       {
                           string CorrelationId { get; }
                       }

                       public class GetOnlySagaData : ContainSagaData, IHasCorrelationId
                       {
                           public string CorrelationId { get; {{setter}} }
                       }

                       [Saga]
                       public class GetOnlySaga : Saga<GetOnlySagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<GetOnlySagaData> mapper) =>
                               mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       public class Start : ICommand
                       {
                           public string CorrelationId { get; set; }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        AssertCorrelationRoundTrip(assembly, "GetOnlySagaData");
        Assert.That(GeneratedSource(source), Does.Contain("((global::IHasCorrelationId)sagaData).CorrelationId;").And.Contain("Name = \"set_CorrelationId\""));
    }

    [Test]
    public void Default_interface_members_are_read_and_written_through_an_accessible_interface()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public interface IHasId
                       {
                           string Id => "default-value";
                       }

                       public interface IHasCorrelationId
                       {
                           string Stored { get; set; }
                           string CorrelationId { get => Stored; set => Stored = value; }
                       }

                       public class DefaultSagaData : ContainSagaData, IHasCorrelationId
                       {
                           public string Stored { get; set; }
                       }

                       public class Start : ICommand, IHasId
                       {
                       }

                       [Saga]
                       public class DefaultSaga : Saga<DefaultSagaData>, IAmStartedByMessages<Start>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DefaultSagaData> mapper) =>
                               mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<Start>(m => ((IHasId)m).Id);

                           public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       """;

        var assembly = CompileAndLoad(source);

        Assert.That(GetAccessor<MessagePropertyAccessor>(assembly).AccessFrom(Activator.CreateInstance(assembly.GetType("Start")!)!), Is.EqualTo("default-value"));

        var sagaData = (IContainSagaData)Activator.CreateInstance(assembly.GetType("DefaultSagaData")!)!;
        var accessor = GetAccessor<CorrelationPropertyAccessor>(assembly);
        accessor.WriteTo(sagaData, "correlation-value");

        Assert.That(sagaData.GetType().GetProperty("Stored")!.GetValue(sagaData), Is.EqualTo("correlation-value"));
        Assert.That(accessor.AccessFrom(sagaData), Is.EqualTo("correlation-value"));
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

    [Test]
    public void Correlation_property_with_a_default_implementation_in_an_inaccessible_interface_gets_no_generated_accessor()
    {
        var source = $$"""
                       {{AddAllPreamble}}

                       public class Outer
                       {
                           interface IHasCorrelationId
                           {
                               string Stored { get; set; }
                               string CorrelationId { get => Stored; set => Stored = value; }
                           }

                           public class DefaultSagaData : ContainSagaData, IHasCorrelationId
                           {
                               public string Stored { get; set; }
                           }

                           [Saga]
                           public class DefaultSaga : Saga<DefaultSagaData>, IAmStartedByMessages<Start>
                           {
                               protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DefaultSagaData> mapper) =>
                                   mapper.MapSaga(s => ((IHasCorrelationId)s).CorrelationId).ToMessage<Start>(m => m.CorrelationId);

                               public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;
                           }

                           public class Start : ICommand
                           {
                               public string CorrelationId { get; set; }
                           }
                       }
                       """;

        var assembly = CompileAndLoad(source);

        Assert.That(GetAccessors<CorrelationPropertyAccessor>(assembly), Is.Empty);
        Assert.That(GetAccessors<MessagePropertyAccessor>(assembly), Has.Length.EqualTo(1));
        Assert.That(GeneratedSource(source), Does.Contain("(associatedMessages, null, propertyAccessors)"));
    }

    static string GeneratedSource(string source, string referencedSource = null) =>
        string.Join(Environment.NewLine, RunGenerators(source, new CSharpParseOptions(LanguageVersion.Preview), CompileReferencedAssembly(referencedSource)).SyntaxTrees.Select(t => t.ToString()));

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

    static SagaMetadata RegisteredSagaMetadata(Assembly assembly, string sagaTypeName)
    {
        var configuration = new EndpointConfiguration("GeneratedAccessors");
        var test = assembly.GetType("Test")!;
        test.GetMethod("Configure")!.Invoke(Activator.CreateInstance(test), [configuration]);
        return configuration.GetSettings().Get<SagaMetadataCollection>().Find(assembly.GetType(sagaTypeName)!);
    }

    static MessagePropertyAccessor RegisteredMessageAccessor(Assembly assembly, string sagaTypeName, string messageTypeName)
    {
        var metadata = RegisteredSagaMetadata(assembly, sagaTypeName);
        Assert.That(metadata.TryGetFinder(assembly.GetType(messageTypeName)!.FullName!, out var finderDefinition), Is.True);

        // The property finder holds the accessor the saga was registered with, generated or compiled from the mapping expression.
        var finder = typeof(SagaFinderDefinition).GetProperty("SagaFinder", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(finderDefinition)!;
        return finder.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(f => f.GetValue(finder)).OfType<MessagePropertyAccessor>().Single();
    }

    // Registering a saga that maps saga data through an interface throws at runtime, so the generated registration is read instead.
    static CorrelationPropertyAccessor RegisteredCorrelationAccessor(Assembly assembly, string source, string sagaTypeName)
    {
        var registration = Regex.Match(GeneratedSource(source), $@"SagaMetadata\.Create<global::{Regex.Escape(sagaTypeName)}, [^>]+>\(associatedMessages, (\w+)\.Instance");
        Assert.That(registration.Success, Is.True, $"{sagaTypeName} is not registered with a generated correlation accessor.");
        return GetAccessors<CorrelationPropertyAccessor>(assembly).Single(accessor => accessor.GetType().Name.EndsWith(registration.Groups[1].Value, StringComparison.Ordinal));
    }

    static void AssertCorrelationRoundTrip(Assembly assembly, string sagaDataTypeName)
    {
        var accessor = GetAccessor<CorrelationPropertyAccessor>(assembly);
        var sagaData = (IContainSagaData)Activator.CreateInstance(assembly.GetType(sagaDataTypeName)!)!;

        accessor.WriteTo(sagaData, "correlation-value");

        Assert.That(accessor.AccessFrom(sagaData), Is.EqualTo("correlation-value"));
    }

    static void AssertExplicitCorrelationRoundTrip(Assembly assembly, string sagaDataTypeName, string interfaceName)
    {
        var accessor = GetAccessor<CorrelationPropertyAccessor>(assembly);
        var sagaData = (IContainSagaData)Activator.CreateInstance(assembly.GetType(sagaDataTypeName)!)!;
        var publicProperty = sagaData.GetType().GetProperty("CorrelationId", BindingFlags.Public | BindingFlags.Instance)!;
        var explicitProperty = sagaData.GetType().GetInterface(interfaceName)!.GetProperty("CorrelationId")!;

        accessor.WriteTo(sagaData, "correlation-value");
        publicProperty.SetValue(sagaData, "public-value");

        Assert.That(explicitProperty.GetValue(sagaData), Is.EqualTo("correlation-value"));
        Assert.That(accessor.AccessFrom(sagaData), Is.EqualTo("correlation-value"));
    }

    static Assembly CompileAndLoad(string source, bool dropMessageHierarchies = false, string referencedSource = null)
    {
        var referencedImage = CompileReferencedAssembly(referencedSource);
        var outputCompilation = RunGenerators(source, new CSharpParseOptions(LanguageVersion.Preview), referencedImage);

        if (dropMessageHierarchies)
        {
            // The registration code lists every interface of a message, which cannot compile for an inaccessible one; only the accessors are under test.
            var registrationTree = outputCompilation.SyntaxTrees.Single(t => t.ToString().Contains("RegisterMessageTypeWithHierarchy"));
            var withoutHierarchies = Regex.Replace(registrationTree.ToString(), @"(RegisterMessageTypeWithHierarchy\(typeof\([^)]*\)), \[[^\]]*\]\)", "$1, [])");
            outputCompilation = outputCompilation.ReplaceSyntaxTree(registrationTree, CSharpSyntaxTree.ParseText(withoutHierarchies, (CSharpParseOptions)registrationTree.Options, registrationTree.FilePath));
        }

        var image = Emit(outputCompilation);
        if (referencedImage is null)
        {
            return Assembly.Load(image);
        }

        // One context for both so the compiled assembly resolves its reference by name.
        var loadContext = new AssemblyLoadContext(null);
        loadContext.LoadFromStream(new MemoryStream(referencedImage));
        return loadContext.LoadFromStream(new MemoryStream(image));
    }

    static byte[] CompileReferencedAssembly(string referencedSource) =>
        referencedSource is null
            ? null
            : Emit(CSharpCompilation.Create("ReferencedSagaData", [CSharpSyntaxTree.ParseText(referencedSource)], ReferenceAssemblyPaths(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)));

    static byte[] Emit(Compilation compilation)
    {
        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);

        var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));

        return peStream.ToArray();
    }

    static Compilation RunGenerators(string source, CSharpParseOptions parseOptions, byte[] referencedImage = null)
    {
        var sourceTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            "CollidingAccessors",
            [sourceTree],
            [.. ReferenceAssemblyPaths(), .. referencedImage is null ? [] : (MetadataReference[])[MetadataReference.CreateFromImage(referencedImage)]],
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
