namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Sagas;
using NUnit.Framework;

// Compiles saga mappings of many shapes in batches and compares each registered accessor with the accessor the runtime compiles from the same mapping.
[TestFixture]
public class GeneratedAccessorDifferentialTests
{
    [TestCaseSource(nameof(CaseNames))]
    public void Registered_accessors_behave_like_the_accessors_compiled_from_the_mapping(string caseName) => AssertOutcome(caseName);

    [TestCaseSource(nameof(KnownLimitationCaseNames))]
    public void Registered_accessors_read_the_mapped_type_implementation_of_a_private_interface_a_derived_message_reimplements(string caseName)
    {
        AssertOutcome(caseName);
        Assert.That(Outcomes.Value[caseName].GeneratedMessageAccessor, Is.True);
    }

    static void AssertOutcome(string caseName)
    {
        var outcome = Outcomes.Value[caseName];
        if (outcome.RejectedBy is { } diagnostics)
        {
            Assert.Pass($"Out of contract, the analyzers reject the mapping: {diagnostics}");
        }

        Assert.That(outcome.Failures, Is.Empty, outcome.Details);
    }

    [Test]
    public void Analyzer_valid_cases_cover_every_dimension()
    {
        var cases = AllCases().ToArray();
        var executed = cases.Where(c => Outcomes.Value[c.Name].RejectedBy is null).ToArray();

        TestContext.Out.WriteLine($"{cases.Length} cases, {executed.Length} analyzer-valid, {cases.Length - executed.Length} rejected by the analyzers in {RunDuration.TotalSeconds:F1}s");
        foreach (var rejection in cases.Select(c => Outcomes.Value[c.Name].RejectedBy).OfType<string>().GroupBy(DiagnosticIds).OrderByDescending(g => g.Count()))
        {
            TestContext.Out.WriteLine($"  {rejection.Count(),5} rejected with {rejection.Key}");
        }

        TestContext.Out.WriteLine($"  {executed.Count(c => Outcomes.Value[c.Name].GeneratedMessageAccessor)} generated message accessors, {executed.Count(c => Outcomes.Value[c.Name].GeneratedCorrelationAccessor)} generated correlation accessors");

        var uncovered = cases.SelectMany(c => c.Dimensions).Distinct()
            .Where(dimension => !executed.Any(c => c.Dimensions.Contains(dimension)))
            .Except(AlwaysRejectedDimensions)
            .ToArray();

        Assert.That(uncovered, Is.Empty, "Every dimension value needs at least one analyzer-valid case.");
    }

    static string DiagnosticIds(string diagnostics) => string.Join(", ", Regex.Matches(diagnostics, @"error (\w+):").Select(match => match.Groups[1].Value).Distinct().Order());

    // Saga data setters must be public.
    static readonly string[] AlwaysRejectedDimensions = ["GetPrivateSet", "GetProtectedSet"];

    static IEnumerable<TestCaseData> CaseNames() => AllCases().Where(c => !c.ReadsMappedTypeImplementationOfDerived).Select(c => new TestCaseData(c.Name).SetArgDisplayNames(c.Name));

    static IEnumerable<TestCaseData> KnownLimitationCaseNames() => AllCases().Where(c => c.ReadsMappedTypeImplementationOfDerived).Select(c => new TestCaseData(c.Name).SetArgDisplayNames(c.Name));

    static IEnumerable<DifferentialCase> AllCases()
    {
        var index = 0;
        foreach (var (name, dimensions, body, sagaType) in MessageCases().Concat(CorrelationCases()))
        {
            var caseNamespace = $"Case{index++:D4}";
            yield return new DifferentialCase(name, dimensions, caseNamespace, $"namespace {caseNamespace}\n{{\n{Suppressions}\n{body}\n#pragma warning restore\n}}\n", $"{caseNamespace}.{sagaType}");
        }
    }

    // What the user suppresses for their own mapping and declarations; obsolete errors and IDs a pragma can't name need an obsolete saga instead.
    const string Suppressions = "#pragma warning disable CS0612, CS0618, CS0628, CS0672, CS0809, CS8602, LEGACY001, EXP001";

    static IEnumerable<(string Name, string[] Dimensions, string Body, string SagaType)> MessageCases()
    {
        var shape = 0;
        var variant = 0;
        foreach (var kind in Enum.GetValues<MessageKind>())
        {
            foreach (var declaration in Enum.GetValues<MessageDeclaration>())
            {
                foreach (var receiver in Enum.GetValues<Receiver>())
                {
                    // Syntax the parser strips, and receivers it generates nothing for, only need a sample.
                    var sparse = receiver is Receiver.Parenthesized or Receiver.NullForgiving or Receiver.CastToInterfaceNullForgiving or Receiver.BoxedResult or Receiver.AsInterface;
                    foreach (var access in Enum.GetValues<MessageAccess>())
                    {
                        if (!IsPossible(kind, declaration, receiver, access) || (sparse && access != MessageAccess.Public)
                            || (access == MessageAccess.Internal && kind is not (MessageKind.Class or MessageKind.SealedClass or MessageKind.AbstractBase)))
                        {
                            continue;
                        }

                        // An attribute needs nothing, a pragma, or an extern; shapes get one of each with the pragma ones rotating, or just none and extern where only sampled.
                        var externAttribute = shape % 3 == 0 ? MemberAttribute.ObsoleteInvalidId : MemberAttribute.ObsoleteError;
                        MemberAttribute[] attributes = sparse || access != MessageAccess.Public || kind is MessageKind.NestedType or MessageKind.Record
                            ? [MemberAttribute.None, externAttribute]
                            : [MemberAttribute.None, RotatingAttributes[shape % RotatingAttributes.Length], externAttribute];
                        for (var index = 0; index < attributes.Length; index++)
                        {
                            var target = attributes[index] == MemberAttribute.None ? AttributeTarget.Property : (AttributeTarget)(((shape / RotatingAttributes.Length) + index) % 2);
                            var property = new Property(RotatingTypes[variant % RotatingTypes.Length], variant % 5 == 0 ? "@event" : "Id");
                            variant++;
                            yield return MessageCase(kind, declaration, receiver, access, attributes[index], target, property);
                        }

                        shape++;
                    }
                }
            }
        }

        yield return MessageCase(MessageKind.Class, MessageDeclaration.OnType, Receiver.Direct, MessageAccess.Public, MemberAttribute.None, AttributeTarget.Property, new Property(PropertyType.NullableInt, "Id"));
    }

    static readonly PropertyType[] RotatingTypes = [PropertyType.String, PropertyType.Guid, PropertyType.Int];
    static readonly MemberAttribute[] RotatingAttributes = [MemberAttribute.Obsolete, MemberAttribute.ObsoleteCustomId, MemberAttribute.ObsoleteCs0618Id, MemberAttribute.Experimental];

    static bool IsPossible(MessageKind kind, MessageDeclaration declaration, Receiver receiver, MessageAccess access)
    {
        var hasDerived = kind != MessageKind.SealedClass;
        var onInterface = declaration is MessageDeclaration.ImplicitInterface or MessageDeclaration.ExplicitInterface or MessageDeclaration.ExplicitInterfaceOnBaseClass or MessageDeclaration.DefaultInterfaceMember
            or MessageDeclaration.ReimplementedExplicitlyInDerived or MessageDeclaration.ReimplementedWithNewInDerived;

        if (!hasDerived && declaration is MessageDeclaration.ReimplementedExplicitlyInDerived or MessageDeclaration.ReimplementedWithNewInDerived)
        {
            return false;
        }

        var possibleAccess = access switch
        {
            MessageAccess.Public => true,
            // An internal or private interface can't be the message type of a public saga.
            MessageAccess.Internal => !onInterface || kind != MessageKind.Interface,
            MessageAccess.PrivateInterface => onInterface && kind != MessageKind.Interface,
            // The saga is nested in the declaring type, which can't be generic.
            MessageAccess.Private or MessageAccess.ProtectedGetter => declaration is MessageDeclaration.OnBaseClass || (declaration is MessageDeclaration.OnType && kind != MessageKind.ClosedGeneric),
            _ => throw new ArgumentOutOfRangeException(nameof(access))
        };

        if (!possibleAccess)
        {
            return false;
        }

        var messageRole = MessageRole(kind);
        var receiverRole = receiver switch
        {
            Receiver.CastToBaseClass => Role.Base,
            Receiver.CastToInterface or Receiver.CastToInterfaceNullForgiving or Receiver.AsInterface => Role.Interface,
            Receiver.CastToMessageClass => Role.Message,
            Receiver.CastToDerived => Role.Derived,
            Receiver.Direct or Receiver.Parenthesized or Receiver.NullForgiving or Receiver.BoxedResult => messageRole,
            _ => throw new ArgumentOutOfRangeException(nameof(receiver))
        };

        if (receiverRole == Role.Derived && !hasDerived)
        {
            return false;
        }

        if (receiverRole == messageRole && receiver is not (Receiver.Direct or Receiver.Parenthesized or Receiver.NullForgiving or Receiver.BoxedResult))
        {
            return false;
        }

        return declaration switch
        {
            MessageDeclaration.OnType => receiverRole is Role.Message or Role.Derived,
            MessageDeclaration.OnBaseClass or MessageDeclaration.GenericBaseClass or MessageDeclaration.HiddenInDerived or MessageDeclaration.OverriddenInDerived => receiverRole is not Role.Interface,
            MessageDeclaration.ImplicitInterface or MessageDeclaration.ReimplementedExplicitlyInDerived or MessageDeclaration.ReimplementedWithNewInDerived => receiverRole is not Role.Base,
            MessageDeclaration.ExplicitInterface or MessageDeclaration.ExplicitInterfaceOnBaseClass or MessageDeclaration.DefaultInterfaceMember => receiverRole is Role.Interface,
            _ => throw new ArgumentOutOfRangeException(nameof(declaration))
        };
    }

    static Role MessageRole(MessageKind kind) => kind == MessageKind.AbstractBase ? Role.Base : kind == MessageKind.Interface ? Role.Interface : Role.Message;

    static (string, string[], string, string) MessageCase(MessageKind kind, MessageDeclaration declaration, Receiver receiver, MessageAccess access, MemberAttribute attribute, AttributeTarget target, Property property)
    {
        var host = access == MessageAccess.PrivateInterface ? "Host." : "";
        var keyword = kind == MessageKind.Record ? "record" : "class";
        var baseDeclaration = declaration == MessageDeclaration.GenericBaseClass ? "GenericBase<TValue>" : "Base";
        var baseType = declaration == MessageDeclaration.GenericBaseClass ? $"GenericBase<{property.Type}>" : "Base";
        var messageType = kind == MessageKind.ClosedGeneric ? "Envelope<Order>" : kind == MessageKind.NestedType ? "Outer.Msg" : "Msg";
        var mappedType = host + MessageRole(kind) switch
        {
            Role.Base => baseType,
            Role.Interface => "IFace",
            Role.Message => messageType,
            Role.Derived or _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var hasDerived = kind != MessageKind.SealedClass;
        var hasOther = kind is MessageKind.AbstractBase or MessageKind.Interface;

        var attributeText = AttributeText(attribute);
        var memberAttribute = target == AttributeTarget.Property ? attributeText : "";
        var getterAttribute = target == AttributeTarget.Getter ? attributeText : "";
        var modifiers = access == MessageAccess.Internal ? "internal" : access == MessageAccess.Private ? "private" : "public";
        var getterModifier = access == MessageAccess.ProtectedGetter ? "protected " : "";

        string AutoProperty(string propertyModifiers, string tag, string accessorModifier = "", string type = null, string value = null) =>
            $"{memberAttribute} {propertyModifiers} {type ?? property.Type} {property.Name} {{ {getterAttribute} {accessorModifier}get; set; }} = {value ?? property.Literal(tag)};";
        string GetterOnly(string propertyModifiers, string tag) => $"{memberAttribute} {propertyModifiers} {property.Type} {property.Name} {{ {getterAttribute} get => {property.Literal(tag)}; }}";
        string Explicit(string tag) => $"{memberAttribute} {property.Type} IFace.{property.Name} {{ {getterAttribute} get => {property.Literal(tag)}; }}";
        var interfaceMember = $"{memberAttribute} {property.Type} {property.Name} {{ {getterAttribute} get; }}";

        string interfaceMembers = "", baseMembers = "", messageMembers = "", derivedMembers = "", otherMembers = "";
        switch (declaration)
        {
            case MessageDeclaration.OnType:
                messageMembers = AutoProperty(modifiers, "Msg", getterModifier);
                break;
            case MessageDeclaration.OnBaseClass:
                baseMembers = AutoProperty(modifiers, "Base", getterModifier);
                break;
            case MessageDeclaration.ImplicitInterface:
                interfaceMembers = interfaceMember;
                messageMembers = AutoProperty("public", "Msg");
                otherMembers = AutoProperty("public", "Other");
                break;
            case MessageDeclaration.ExplicitInterface:
                interfaceMembers = interfaceMember;
                messageMembers = Explicit("Msg");
                otherMembers = Explicit("Other");
                break;
            case MessageDeclaration.ExplicitInterfaceOnBaseClass:
                interfaceMembers = interfaceMember;
                baseMembers = Explicit("Base");
                break;
            case MessageDeclaration.DefaultInterfaceMember:
                interfaceMembers = $"{memberAttribute} {property.Type} {property.Name} {{ {getterAttribute} get => {property.Literal("IFace")}; }}";
                break;
            case MessageDeclaration.GenericBaseClass:
                baseMembers = AutoProperty(modifiers, "Base", type: "TValue", value: $"(TValue)(object){property.Literal("Base")}");
                break;
            case MessageDeclaration.HiddenInDerived:
                baseMembers = AutoProperty(modifiers, "Base");
                messageMembers = AutoProperty($"{modifiers} new", "Msg");
                derivedMembers = AutoProperty($"{modifiers} new", "Derived");
                break;
            case MessageDeclaration.OverriddenInDerived:
                baseMembers = AutoProperty($"{modifiers} virtual", "Base");
                messageMembers = GetterOnly($"{modifiers} override", "Msg");
                derivedMembers = GetterOnly($"{modifiers} override", "Derived");
                otherMembers = GetterOnly($"{modifiers} override", "Other");
                break;
            case MessageDeclaration.ReimplementedExplicitlyInDerived:
                interfaceMembers = interfaceMember;
                messageMembers = AutoProperty("public", "Msg");
                derivedMembers = Explicit("Derived");
                otherMembers = AutoProperty("public", "Other");
                break;
            case MessageDeclaration.ReimplementedWithNewInDerived:
                interfaceMembers = interfaceMember;
                messageMembers = AutoProperty("public", "Msg");
                derivedMembers = AutoProperty("public new", "Derived");
                otherMembers = AutoProperty("public", "Other");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(declaration));
        }

        var saga = Saga(mappedType, "CorrelationId", MappingExpression(receiver, property, host, baseType, messageType), attribute);
        var sagaHost = access switch
        {
            MessageAccess.Private or MessageAccess.ProtectedGetter when declaration == MessageDeclaration.OnType => kind == MessageKind.NestedType ? "Outer+Msg" : "Msg",
            MessageAccess.Private or MessageAccess.ProtectedGetter => "Base",
            MessageAccess.PrivateInterface => "Host",
            MessageAccess.Public or MessageAccess.Internal => null,
            _ => throw new ArgumentOutOfRangeException(nameof(access))
        };

        var interfaceAccess = access == MessageAccess.PrivateInterface ? "private" : access == MessageAccess.Internal && interfaceMembers != "" ? "internal" : "public";
        var interfaceBase = kind == MessageKind.Interface ? " : ICommand" : "";
        var baseInterfaces = declaration == MessageDeclaration.ExplicitInterfaceOnBaseClass ? ", IFace" : "";
        var derivedInterfaces = declaration is MessageDeclaration.ReimplementedExplicitlyInDerived or MessageDeclaration.ReimplementedWithNewInDerived ? ", IFace" : "";

        var messageDeclaration = $$"""
                                   public {{(kind == MessageKind.SealedClass ? "sealed " : "")}}{{keyword}} {{(kind == MessageKind.ClosedGeneric ? "Envelope<TPayload>" : "Msg")}} : {{baseType}}, IFace
                                   {
                                       {{messageMembers}}
                                       {{(sagaHost is "Msg" or "Outer+Msg" ? saga : "")}}
                                   }
                                   """;
        var types = $$"""
                      public {{(kind == MessageKind.AbstractBase ? "abstract " : "")}}{{keyword}} {{baseDeclaration}} : ICommand{{baseInterfaces}}
                      {
                          {{baseMembers}}
                          {{(sagaHost == "Base" ? saga : "")}}
                      }

                      {{interfaceAccess}} interface IFace{{interfaceBase}}
                      {
                          {{interfaceMembers}}
                      }

                      {{(kind == MessageKind.NestedType ? $"public class Outer\n{{\n{messageDeclaration}\n}}" : messageDeclaration)}}

                      {{(hasDerived ? $"public {keyword} Derived : {messageType}{derivedInterfaces} {{ {derivedMembers} }}" : "")}}

                      {{(hasOther ? $"public {keyword} Other : {baseType}, IFace {{ {otherMembers} }}" : "")}}

                      {{(sagaHost == "Host" ? saga : "")}}
                      """;

        var instances = string.Join(", ", new[] { messageType, hasDerived ? "Derived" : null, hasOther ? "Other" : null }.OfType<string>().Select(type => $"new {host}{type}()"));
        var body = $$"""
                     {{(host == "" ? types : $"public class Host\n{{\n{types}\n}}")}}

                     {{(kind == MessageKind.ClosedGeneric ? "public class Order { }" : "")}}

                     {{(sagaHost is null ? saga : "")}}

                     public class TheSagaData : ContainSagaData
                     {
                         public {{property.Type}} CorrelationId { get; set; } = {{property.Literal("Data")}};
                     }

                     {{Probe(instances, property)}}
                     """;

        var name = $"Message_{kind}_{declaration}_{receiver}_{access}_{AttributeName(attribute, target)}_{property}";
        string[] dimensions = [kind.ToString(), declaration.ToString(), receiver.ToString(), access.ToString(), attribute.ToString(), $"{attribute}On{target}", property.TypeDimension, property.NameDimension];
        return (name, dimensions, body, sagaHost is null ? "TheSaga" : $"{sagaHost}+TheSaga");
    }

    static string MappingExpression(Receiver receiver, Property property, string host, string baseType, string messageType) => receiver switch
    {
        Receiver.Direct => $"m.{property.Name}",
        Receiver.Parenthesized => $"(m).{property.Name}",
        Receiver.NullForgiving => $"m!.{property.Name}",
        Receiver.BoxedResult => $"(object)m.{property.Name}",
        Receiver.CastToBaseClass => $"(({host}{baseType})m).{property.Name}",
        Receiver.CastToInterface => $"(({host}IFace)m).{property.Name}",
        Receiver.CastToInterfaceNullForgiving => $"(({host}IFace)m)!.{property.Name}",
        Receiver.AsInterface => $"(m as {host}IFace).{property.Name}",
        Receiver.CastToMessageClass => $"(({host}{messageType})m).{property.Name}",
        Receiver.CastToDerived => $"(({host}Derived)m).{property.Name}",
        _ => throw new ArgumentOutOfRangeException(nameof(receiver))
    };

    static IEnumerable<(string Name, string[] Dimensions, string Body, string SagaType)> CorrelationCases()
    {
        var variant = 0;
        foreach (var declaration in Enum.GetValues<SagaDataDeclaration>())
        {
            foreach (var accessors in Enum.GetValues<SagaDataAccessors>())
            {
                if ((declaration == SagaDataDeclaration.OverriddenOnSagaData && accessors is not (SagaDataAccessors.GetSet or SagaDataAccessors.GetInit))
                    || (declaration == SagaDataDeclaration.OnGenericBaseClass && accessors == SagaDataAccessors.PrivateGetSet))
                {
                    continue;
                }

                foreach (var attribute in Enum.GetValues<MemberAttribute>())
                {
                    foreach (var target in Enum.GetValues<AttributeTarget>())
                    {
                        if (attribute == MemberAttribute.None && target != AttributeTarget.Property)
                        {
                            continue;
                        }

                        var property = new Property(RotatingTypes[variant % RotatingTypes.Length], variant % 5 == 0 ? "@event" : "OrderId");
                        variant++;
                        yield return CorrelationCase(declaration, accessors, attribute, target, property);
                    }
                }
            }
        }

        yield return CorrelationCase(SagaDataDeclaration.OnSagaData, SagaDataAccessors.GetSet, MemberAttribute.None, AttributeTarget.Property, new Property(PropertyType.NullableInt, "OrderId"));
    }

    static (string, string[], string, string) CorrelationCase(SagaDataDeclaration declaration, SagaDataAccessors accessors, MemberAttribute attribute, AttributeTarget target, Property property)
    {
        var attributeText = AttributeText(attribute);
        var getterModifier = accessors == SagaDataAccessors.PrivateGetSet ? "private " : "";
        var setterModifier = accessors == SagaDataAccessors.GetPrivateSet ? "private " : accessors == SagaDataAccessors.GetProtectedSet ? "protected " : "";
        var setter = accessors == SagaDataAccessors.GetInit ? "init" : "set";

        string Declare(string modifiers, string type, string value) =>
            $"{(target == AttributeTarget.Property ? attributeText : "")} public {modifiers} {type} {property.Name} {{ {(target == AttributeTarget.Getter ? attributeText : "")} {getterModifier}get; {(target == AttributeTarget.Setter ? attributeText : "")} {setterModifier}{setter}; }} = {value};";

        var mapping = $"m.{property.Name}";
        var message = $"public class Msg : ICommand {{ public {property.Type} {property.Name} {{ get; set; }} = {property.Literal("Msg")}; }}";
        var nestSaga = accessors == SagaDataAccessors.PrivateGetSet;
        var saga = Saga("Msg", property.Name, mapping, attribute);
        var (sagaData, sagaHost) = declaration switch
        {
            SagaDataDeclaration.OnSagaData => ($"public class TheSagaData : ContainSagaData {{ {Declare("", property.Type, property.Literal("Data"))} {(nestSaga ? saga : "")} }}", "TheSagaData"),
            SagaDataDeclaration.OnBaseClass => ($"public class DataBase : ContainSagaData {{ {Declare("", property.Type, property.Literal("Data"))} {(nestSaga ? saga : "")} }} public class TheSagaData : DataBase {{ }}", "DataBase"),
            SagaDataDeclaration.OnGenericBaseClass => ($"public class DataBase<TValue> : ContainSagaData {{ {Declare("", "TValue", $"(TValue)(object){property.Literal("Data")}")} }} public class TheSagaData : DataBase<{property.Type}> {{ }}", null),
            SagaDataDeclaration.OverriddenOnSagaData => ($"public class DataBase : ContainSagaData {{ {Declare("virtual", property.Type, property.Literal("Base"))} }} public class TheSagaData : DataBase {{ {Declare("override", property.Type, property.Literal("Data"))} }}", null),
            _ => throw new ArgumentOutOfRangeException(nameof(declaration))
        };

        var body = $"""
                    {message}

                    {sagaData}

                    {(nestSaga ? "" : saga)}

                    {Probe("new Msg()", property)}
                    """;

        var name = $"Correlation_{declaration}_{accessors}_{AttributeName(attribute, target)}_{property}";
        string[] dimensions = [declaration.ToString(), accessors.ToString(), attribute.ToString(), $"{attribute}On{target}", property.TypeDimension, property.NameDimension];
        return (name, dimensions, body, nestSaga ? $"{sagaHost}+TheSaga" : "TheSaga");
    }

    static string Saga(string mappedType, string sagaProperty, string mapping, MemberAttribute attribute) =>
        $$"""
          [Saga]
          {{(attribute is MemberAttribute.ObsoleteError or MemberAttribute.ObsoleteInvalidId ? "[System.Obsolete]" : "")}}
          public class TheSaga : Saga<TheSagaData>, IAmStartedByMessages<{{mappedType}}>
          {
              protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TheSagaData> mapper) =>
                  mapper.MapSaga(s => s.{{sagaProperty}}).ToMessage<{{mappedType}}>(m => {{mapping}});

              public Task Handle({{mappedType}} message, IMessageHandlerContext context) => Task.CompletedTask;
          }
          """;

    static string Probe(string instances, Property property) =>
        $$"""
          public static class Probe
          {
              public static object[] Messages() => [{{instances}}];

              public static object[] Values() => [{{property.Literal("First")}}, {{property.Literal("Second")}}];
          }
          """;

    static string AttributeText(MemberAttribute attribute) => attribute switch
    {
        MemberAttribute.None => "",
        MemberAttribute.Obsolete => "[System.Obsolete(\"Use something else\")]",
        MemberAttribute.ObsoleteCustomId => "[System.Obsolete(\"Use something else\", DiagnosticId = \"LEGACY001\")]",
        MemberAttribute.ObsoleteCs0618Id => "[System.Obsolete(\"Use something else\", DiagnosticId = \"CS0618\")]",
        MemberAttribute.ObsoleteError => "[System.Obsolete(\"Use something else\", true)]",
        MemberAttribute.Experimental => "[System.Diagnostics.CodeAnalysis.Experimental(\"EXP001\")]",
        MemberAttribute.ObsoleteInvalidId => "[System.Obsolete(\"Use something else\", DiagnosticId = \"LEGACY-001\")]",
        _ => throw new ArgumentOutOfRangeException(nameof(attribute))
    };

    static string AttributeName(MemberAttribute attribute, AttributeTarget target) => attribute == MemberAttribute.None ? "NoAttribute" : $"{attribute}On{target}";

    static readonly Lazy<IReadOnlyDictionary<string, Outcome>> Outcomes = new(Run);
    static TimeSpan RunDuration;

    static ConcurrentDictionary<string, Outcome> Run()
    {
        var stopwatch = Stopwatch.StartNew();
        // Only what the cases use, because the analyzers visit every type the compilation references.
        var references = SagaAccessorCompilation.ReferenceAssemblyPaths()
            .Where(reference => Regex.IsMatch(Path.GetFileName(reference.Display!), @"^(System\.Private\.CoreLib|System\.Runtime|netstandard|System\.Collections|System\.Linq\.Expressions|NServiceBus\.Core|NServiceBus\.MessageInterfaces|Microsoft\.Extensions\.DependencyInjection\.Abstractions)\.dll$"))
            .ToArray();
        var outcomes = new ConcurrentDictionary<string, Outcome>();
        Parallel.ForEach(AllCases().Chunk(50), chunk => RunChunk(chunk, references, outcomes));
        RunDuration = stopwatch.Elapsed;
        return outcomes;
    }

    static void RunChunk(DifferentialCase[] chunk, MetadataReference[] references, ConcurrentDictionary<string, Outcome> outcomes)
    {
        var remaining = chunk.ToDictionary(c => c.Namespace);

        var diagnostics = Compile(remaining.Values, references).WithAnalyzers(Analyzers).GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        var analyzerIds = Analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).Select(descriptor => descriptor.Id).ToHashSet();

        // The registration methods only exist once the generators ran, so only errors inside the cases count here.
        _ = Remove(remaining, outcomes, diagnostics.Where(d => !analyzerIds.Contains(d.Id) && IsReported(d)), (c, compilerDiagnostics) => Outcome.Failed(c, $"The case source doesn't compile:\n{compilerDiagnostics}"));

        // Warnings as errors raises the reported severity, so analyzer errors are told apart by their default severity.
        FailRemaining(remaining, outcomes, Remove(remaining, outcomes, diagnostics.Where(d => analyzerIds.Contains(d.Id) && d.Descriptor.DefaultSeverity == DiagnosticSeverity.Error), Outcome.Rejected));

        GenerateAndExecute(remaining, references, outcomes);
    }

    static void GenerateAndExecute(Dictionary<string, DifferentialCase> remaining, MetadataReference[] references, ConcurrentDictionary<string, Outcome> outcomes)
    {
        while (remaining.Count > 0)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            var compilation = SagaAccessorCompilation.RunGenerators(Compile(remaining.Values, references), parseOptions, out var generatorDiagnostics);
            // Messages implementing a private interface hit Particular/NServiceBus#7968 in the message registration, which isn't under test.
            compilation = SagaAccessorCompilation.WithoutMessageHierarchies(compilation);

            using var peStream = new MemoryStream();
            var emitResult = compilation.Emit(peStream);
            var reported = generatorDiagnostics.Concat(emitResult.Diagnostics).Where(IsReported).ToArray();
            if (reported.Length == 0)
            {
                var assembly = Assembly.Load(peStream.ToArray());
                var configuration = new EndpointConfiguration("GeneratedAccessors");
                foreach (var differentialCase in remaining.Values)
                {
                    var outcome = Execute(assembly, configuration, differentialCase);
                    outcomes[differentialCase.Name] = outcome.Failures.Count == 0 ? outcome : outcome with { Details = $"{outcome.Details}\n\nGenerated accessors:\n{GeneratedSourceOf(compilation, differentialCase)}" };
                }

                return;
            }

            var unattributed = Remove(remaining, outcomes, reported, (c, diagnostics) => Outcome.Failed(c, $"The generated code doesn't compile:\n{diagnostics}\n\n{GeneratedSourceOf(compilation, c)}"));
            if (unattributed.Count == 0)
            {
                continue;
            }

            if (remaining.Count == 1)
            {
                FailRemaining(remaining, outcomes, unattributed);
                return;
            }

            // A syntax error in generated code can't be traced to a case, so halve the batch until it can.
            foreach (var half in remaining.Values.Chunk((remaining.Count + 1) / 2).ToArray())
            {
                GenerateAndExecute(half.ToDictionary(c => c.Namespace), references, outcomes);
            }

            return;
        }
    }

    static void FailRemaining(Dictionary<string, DifferentialCase> remaining, ConcurrentDictionary<string, Outcome> outcomes, List<Diagnostic> unattributed)
    {
        if (unattributed.Count == 0)
        {
            return;
        }

        foreach (var differentialCase in remaining.Values)
        {
            outcomes[differentialCase.Name] = Outcome.Failed(differentialCase, $"Diagnostics outside of any case:\n{string.Join("\n", unattributed)}");
        }

        remaining.Clear();
    }

    static bool IsReported(Diagnostic diagnostic) => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;

    static List<Diagnostic> Remove(Dictionary<string, DifferentialCase> remaining, ConcurrentDictionary<string, Outcome> outcomes, IEnumerable<Diagnostic> diagnostics, Func<DifferentialCase, string, Outcome> outcome)
    {
        var byCase = new Dictionary<string, List<Diagnostic>>();
        var unattributed = new List<Diagnostic>();
        foreach (var diagnostic in diagnostics)
        {
            var caseNamespaces = CaseNamespacesOf(diagnostic).Where(remaining.ContainsKey).ToArray();
            if (caseNamespaces.Length == 0)
            {
                unattributed.Add(diagnostic);
            }

            foreach (var caseNamespace in caseNamespaces)
            {
                (byCase.TryGetValue(caseNamespace, out var list) ? list : byCase[caseNamespace] = []).Add(diagnostic);
            }
        }

        foreach (var (caseNamespace, caseDiagnostics) in byCase)
        {
            var differentialCase = remaining[caseNamespace];
            outcomes[differentialCase.Name] = outcome(differentialCase, string.Join("\n", caseDiagnostics.Select(d => d.ToString()).Distinct()));
            remaining.Remove(caseNamespace);
        }

        return unattributed;
    }

    static readonly Regex CaseNamespacePattern = new(@"\bCase\d{4}\b", RegexOptions.Compiled);

    // A diagnostic in a case is inside its namespace; one in generated code is in the nearest declaration or statement that names a case.
    static IEnumerable<string> CaseNamespacesOf(Diagnostic diagnostic)
    {
        if (diagnostic.Location.SourceTree is not { } tree)
        {
            return [];
        }

        var inGeneratedCode = !string.IsNullOrEmpty(tree.FilePath);

        for (var node = tree.GetRoot().FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true); node is not null; node = node.Parent)
        {
            if (node is NamespaceDeclarationSyntax { Name: var name } && CaseNamespacePattern.IsMatch(name.ToString()))
            {
                return [name.ToString()];
            }

            if (node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            {
                return [];
            }

            var matches = inGeneratedCode ? CaseNamespacePattern.Matches(node.ToString()) : null;
            if (matches is { Count: > 0 })
            {
                return matches.Select(match => match.Value).Distinct();
            }
        }

        return [];
    }

    static Compilation Compile(IEnumerable<DifferentialCase> cases, MetadataReference[] references)
    {
        var caseArray = cases.ToArray();
        var source = $$"""
                       using System.Threading.Tasks;
                       using NServiceBus;

                       public class Test
                       {
                           public static void Register(EndpointConfiguration cfg, string caseNamespace)
                           {
                               switch (caseNamespace)
                               {
                       {{string.Join("\n", caseArray.Select(c => $"            case \"{c.Namespace}\": cfg.Handlers.{SagaAccessorCompilation.AssemblyName}Assembly.{c.Namespace}.AddAll(); break;"))}}
                               }
                           }
                       }

                       {{string.Join("\n", caseArray.Select(c => c.Source))}}
                       """;
        var compilation = SagaAccessorCompilation.CreateCompilation(source, new CSharpParseOptions(LanguageVersion.Preview), warningsAsErrors: true, references);
        return compilation.WithOptions(compilation.Options.WithConcurrentBuild(false));
    }

    static readonly ImmutableArray<DiagnosticAnalyzer> Analyzers = [new SagaAnalyzer(), new SagaAttributeAnalyzer()];

    static string GeneratedSourceOf(Compilation compilation, DifferentialCase differentialCase) =>
        string.Join("\n\n", compilation.SyntaxTrees
            .Where(tree => !string.IsNullOrEmpty(tree.FilePath))
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Where(type => type.Modifiers.Any(SyntaxKind.FileKeyword) && type.ToString().Contains($"{differentialCase.Namespace}."))
            .Select(type => type.ToString()));

    static Outcome Execute(Assembly assembly, EndpointConfiguration configuration, DifferentialCase differentialCase)
    {
        var failures = new List<string>();
        var outcome = new Outcome(null, failures, differentialCase.Source);
        try
        {
            var sagaType = assembly.GetType(differentialCase.SagaType, throwOnError: true)!;
            var probe = assembly.GetType($"{differentialCase.Namespace}.Probe", throwOnError: true)!;

            var registered = Describe(() => Register(assembly, configuration, differentialCase, sagaType));
            var expected = Describe(() => SagaAccessorCompilation.ExpressionBasedMetadata(sagaType));
            if (registered.Value is not SagaMetadata registeredMetadata || expected.Value is not SagaMetadata expectedMetadata)
            {
                if (registered.Description != expected.Description)
                {
                    failures.Add($"Creating the saga metadata: registered {registered.Description}, compiled from the mapping {expected.Description}");
                }

                return outcome;
            }

            var messageType = registeredMetadata.AssociatedMessages.Single().MessageType;
            var registeredMessageAccessor = MessageAccessor(registeredMetadata, messageType);
            var expectedMessageAccessor = MessageAccessor(expectedMetadata, messageType);
            outcome = outcome with { GeneratedMessageAccessor = registeredMessageAccessor.GetType().Assembly == assembly };
            var messages = (object[])probe.GetMethod("Messages")!.Invoke(null, null)!;
            foreach (var message in messages)
            {
                var readAs = differentialCase.ReadsMappedTypeImplementationOfDerived && message.GetType().Name == "Derived" ? messages.Single(m => m.GetType() == messageType) : message;
                Compare(failures, $"Reading a {message.GetType().Name}", () => registeredMessageAccessor.AccessFrom(message), () => expectedMessageAccessor.AccessFrom(readAs));
            }

            if (!registeredMetadata.TryGetCorrelationProperty(out var registeredCorrelation) || !expectedMetadata.TryGetCorrelationProperty(out var expectedCorrelation))
            {
                throw new InvalidOperationException("The saga metadata has no correlation property.");
            }

            var registeredAccessor = registeredCorrelation.Accessor;
            var expectedAccessor = expectedCorrelation.Accessor;
            outcome = outcome with { GeneratedCorrelationAccessor = registeredAccessor.GetType().Assembly == assembly };

            IContainSagaData NewSagaData() => (IContainSagaData)Activator.CreateInstance(registeredMetadata.SagaEntityType)!;
            Compare(failures, "Reading new saga data", () => registeredAccessor.AccessFrom(NewSagaData()), () => expectedAccessor.AccessFrom(NewSagaData()));
            foreach (var value in (object[])probe.GetMethod("Values")!.Invoke(null, null)!)
            {
                var writtenByRegistered = NewSagaData();
                var writtenByExpected = NewSagaData();
                Compare(failures, $"Writing {value}", () => Write(registeredAccessor, writtenByRegistered, value), () => Write(expectedAccessor, writtenByExpected, value));
                Compare(failures, $"Reading {value} written by the registered accessor", () => expectedAccessor.AccessFrom(writtenByRegistered), () => expectedAccessor.AccessFrom(writtenByExpected));
                Compare(failures, $"Reading {value} with the registered accessor", () => registeredAccessor.AccessFrom(writtenByExpected), () => expectedAccessor.AccessFrom(writtenByExpected));
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception.ToString());
        }

        return outcome;
    }

    static SagaMetadata Register(Assembly assembly, EndpointConfiguration configuration, DifferentialCase differentialCase, Type sagaType)
    {
        assembly.GetType("Test")!.GetMethod("Register")!.Invoke(null, [configuration, differentialCase.Namespace]);
        return configuration.GetSettings().Get<SagaMetadataCollection>().Find(sagaType);
    }

    static MessagePropertyAccessor MessageAccessor(SagaMetadata metadata, Type messageType) =>
        metadata.TryGetFinder(messageType.FullName!, out var finderDefinition)
            ? SagaAccessorCompilation.MessageAccessor(finderDefinition)
            : throw new InvalidOperationException($"The saga metadata has no finder for {messageType}.");

    static object Write(CorrelationPropertyAccessor accessor, IContainSagaData sagaData, object value)
    {
        accessor.WriteTo(sagaData, value);
        return null;
    }

    static void Compare(List<string> failures, string operation, Func<object> registered, Func<object> expected)
    {
        var (_, registeredDescription) = Describe(registered);
        var (_, expectedDescription) = Describe(expected);
        if (registeredDescription != expectedDescription)
        {
            failures.Add($"{operation}: registered accessor {registeredDescription}, accessor compiled from the mapping {expectedDescription}");
        }
    }

    static (object Value, string Description) Describe(Func<object> operation)
    {
        try
        {
            var value = operation();
            return (value, value is SagaMetadata ? "creates the metadata" : $"returns {value ?? "null"} ({value?.GetType().Name})");
        }
        catch (Exception exception)
        {
            var thrown = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            return (null, $"throws {thrown.GetType().Name}");
        }
    }

    sealed record DifferentialCase(string Name, string[] Dimensions, string Namespace, string Source, string SagaType)
    {
        // Known limitation until Particular/NServiceBus#7968 lets such messages register: the generated accessor reads the mapped type's implementation.
        public bool ReadsMappedTypeImplementationOfDerived =>
            Dimensions.Contains(nameof(MessageAccess.PrivateInterface)) && Dimensions.Contains(nameof(Receiver.CastToInterface))
            && Dimensions.Any(dimension => dimension is nameof(MessageDeclaration.ReimplementedExplicitlyInDerived) or nameof(MessageDeclaration.ReimplementedWithNewInDerived))
            && !Dimensions.Contains(nameof(MessageKind.AbstractBase))
            && !(Dimensions.Contains(nameof(MessageKind.ClosedGeneric)) && Dimensions.Any(dimension => dimension is nameof(MemberAttribute.ObsoleteError) or nameof(MemberAttribute.ObsoleteInvalidId)));
    }

    sealed record Outcome(string RejectedBy, IReadOnlyList<string> Failures, string Details)
    {
        public bool GeneratedMessageAccessor { get; init; }
        public bool GeneratedCorrelationAccessor { get; init; }

        public static Outcome Rejected(DifferentialCase differentialCase, string diagnostics) => new(diagnostics, [], differentialCase.Source);

        public static Outcome Failed(DifferentialCase differentialCase, string failure) => new(null, [failure], differentialCase.Source);
    }

    sealed record Property(PropertyType PropertyType, string Name)
    {
        public string Type => PropertyType switch
        {
            PropertyType.String => "string",
            PropertyType.Guid => "System.Guid",
            PropertyType.Int => "int",
            PropertyType.NullableInt => "int?",
            _ => throw new InvalidOperationException()
        };

        public string TypeDimension => PropertyType.ToString();

        public string NameDimension => Name == "@event" ? "KeywordName" : "PlainName";

        public string Literal(string tag)
        {
            var number = Array.IndexOf(Tags, tag) + 1;
            return PropertyType switch
            {
                PropertyType.String => $"\"{tag}\"",
                PropertyType.Guid => $"new System.Guid(\"00000000-0000-0000-0000-{number:D12}\")",
                PropertyType.Int or PropertyType.NullableInt => number.ToString(),
                _ => throw new InvalidOperationException()
            };
        }

        public override string ToString() => $"{PropertyType}{(Name == "@event" ? "_KeywordName" : "")}";

        static readonly string[] Tags = ["Base", "Msg", "Derived", "Other", "IFace", "Data", "First", "Second"];
    }

    enum Role { Base, Interface, Message, Derived }

    enum MessageKind { Class, SealedClass, AbstractBase, Interface, ClosedGeneric, NestedType, Record }

    enum MessageDeclaration { OnType, OnBaseClass, ImplicitInterface, ExplicitInterface, ExplicitInterfaceOnBaseClass, DefaultInterfaceMember, GenericBaseClass, HiddenInDerived, OverriddenInDerived, ReimplementedExplicitlyInDerived, ReimplementedWithNewInDerived }

    enum Receiver { Direct, Parenthesized, NullForgiving, BoxedResult, CastToBaseClass, CastToInterface, CastToInterfaceNullForgiving, AsInterface, CastToMessageClass, CastToDerived }

    enum MessageAccess { Public, Internal, Private, ProtectedGetter, PrivateInterface }

    enum MemberAttribute { None, Obsolete, ObsoleteCustomId, ObsoleteCs0618Id, ObsoleteError, Experimental, ObsoleteInvalidId }

    enum AttributeTarget { Property, Getter, Setter }

    enum PropertyType { String, Guid, Int, NullableInt }

    enum SagaDataDeclaration { OnSagaData, OnBaseClass, OnGenericBaseClass, OverriddenOnSagaData }

    enum SagaDataAccessors { GetSet, GetInit, GetPrivateSet, GetProtectedSet, PrivateGetSet }
}
