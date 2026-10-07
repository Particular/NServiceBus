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

    [TestCaseSource(nameof(RuntimeMessageAccessorCaseNames))]
    public void Registered_accessors_for_receivers_generated_code_cannot_repeat_exactly_are_the_accessors_compiled_from_the_mapping(string caseName)
    {
        AssertOutcome(caseName);
        Assert.That(Outcomes.Value[caseName].GeneratedMessageAccessor, Is.False);
    }

    [TestCaseSource(nameof(UnderscoreNameCaseNames))]
    public void Registered_accessors_for_names_whose_parts_join_the_same_way_are_generated_for_each_saga(string caseName)
    {
        AssertOutcome(caseName);
        Assert.That(Outcomes.Value[caseName].GeneratedMessageAccessors, Is.EqualTo(2));
        Assert.That(Outcomes.Value[caseName].GeneratedCorrelationAccessors, Is.EqualTo(2));
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
        TestContext.Out.WriteLine($"  {executed.Count(c => c.UsesRuntimeMessageAccessor)} receivers generated code can't repeat exactly, {executed.Count(c => c.HasUnderscoreNames)} pairs of names joining the same way");

        var uncovered = cases.SelectMany(c => c.Dimensions).Distinct()
            .Where(dimension => !executed.Any(c => c.Dimensions.Contains(dimension)))
            .Except(AlwaysRejectedDimensions)
            .ToArray();

        Assert.That(uncovered, Is.Empty, "Every dimension value needs at least one analyzer-valid case.");
    }

    static string DiagnosticIds(string diagnostics) => string.Join(", ", Regex.Matches(diagnostics, @"error (\w+):").Select(match => match.Groups[1].Value).Distinct().Order());

    // Saga data setters must be public.
    static readonly string[] AlwaysRejectedDimensions = ["GetPrivateSet", "GetProtectedSet"];

    static IEnumerable<TestCaseData> CaseNames() => CaseNamesWhere(c => !c.ReadsMappedTypeImplementationOfDerived && !c.UsesRuntimeMessageAccessor && !c.HasUnderscoreNames);

    static IEnumerable<TestCaseData> KnownLimitationCaseNames() => CaseNamesWhere(c => c.ReadsMappedTypeImplementationOfDerived);

    static IEnumerable<TestCaseData> RuntimeMessageAccessorCaseNames() => CaseNamesWhere(c => c.UsesRuntimeMessageAccessor);

    static IEnumerable<TestCaseData> UnderscoreNameCaseNames() => CaseNamesWhere(c => c.HasUnderscoreNames);

    static IEnumerable<TestCaseData> CaseNamesWhere(Func<DifferentialCase, bool> predicate) => AllCases().Where(predicate).Select(c => new TestCaseData(c.Name).SetArgDisplayNames(c.Name));

    static IEnumerable<DifferentialCase> AllCases()
    {
        var index = 0;
        foreach (var (name, dimensions, body, sagaTypes) in MessageCases().Concat(MultiSagaCases()).Concat(ConversionOperatorCases()).Concat(RuntimeReceiverCases()).Concat(UnderscoreNameCases()).Concat(CorrelationCases()))
        {
            var caseNamespace = $"Case{index++:D4}";
            yield return new DifferentialCase(name, dimensions, caseNamespace, $"namespace {caseNamespace}\n{{\n{Suppressions}\n{body}\n#pragma warning restore\n}}\n", [.. sagaTypes.Select(sagaType => $"{caseNamespace}.{sagaType}")]);
        }
    }

    // What the user suppresses for their own mapping and declarations; obsolete errors and IDs a pragma can't name need an obsolete saga instead.
    const string Suppressions = "#pragma warning disable CS0612, CS0618, CS0628, CS0672, CS0809, CS8602, LEGACY001, EXP001";

    static IEnumerable<(string Name, string[] Dimensions, string Body, string[] SagaTypes)> MessageCases()
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

                        // Conversions of the read value and cast types generated code may not be able to name only need a sample of the public shapes.
                        if (access == MessageAccess.Public && !sparse)
                        {
                            var conversion = RotatingConversions[shape % RotatingConversions.Length];
                            var convertedType = conversion is OuterConversion.WideningNumeric or OuterConversion.NullableLift ? PropertyType.Int : RotatingTypes[shape % RotatingTypes.Length];
                            yield return MessageCase(kind, declaration, receiver, access, MemberAttribute.None, AttributeTarget.Property, new Property(convertedType, "Id"), new Variation(Conversion: conversion));

                            if (receiver == Receiver.CastToDerived)
                            {
                                var castTarget = RotatingCastTargets[shape % RotatingCastTargets.Length];
                                yield return MessageCase(kind, declaration, receiver, access, MemberAttribute.None, AttributeTarget.Property, new Property(RotatingTypes[shape % RotatingTypes.Length], "Id"), new Variation(CastTarget: castTarget));
                            }
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
    static readonly OuterConversion[] RotatingConversions = [OuterConversion.Identity, OuterConversion.Boxing, OuterConversion.WideningNumeric, OuterConversion.NullableLift];
    static readonly CastTarget[] RotatingCastTargets = [CastTarget.FileLocal, CastTarget.Obsolete, CastTarget.ObsoleteCustomId, CastTarget.ObsoleteError, CastTarget.Experimental];

    // Two sagas mapping the same message through different receivers must each read like their own mapping.
    static IEnumerable<(string Name, string[] Dimensions, string Body, string[] SagaTypes)> MultiSagaCases()
    {
        Receiver[] receivers = [Receiver.Direct, Receiver.BoxedResult, Receiver.CastToBaseClass, Receiver.CastToInterface, Receiver.AsInterface, Receiver.CastToMessageClass, Receiver.CastToDerived];
        var variant = 0;
        foreach (var kind in Enum.GetValues<MessageKind>())
        {
            foreach (var declaration in Enum.GetValues<MessageDeclaration>())
            {
                var possible = receivers.Where(receiver => IsPossible(kind, declaration, receiver, MessageAccess.Public)).ToArray();
                for (var first = 0; first < possible.Length; first++)
                {
                    for (var second = first + 1; second < possible.Length; second++)
                    {
                        var property = new Property(RotatingTypes[variant++ % RotatingTypes.Length], "Id");
                        yield return MessageCase(kind, declaration, possible[first], MessageAccess.Public, MemberAttribute.None, AttributeTarget.Property, property, new Variation(SecondReceiver: possible[second]));
                    }
                }
            }
        }
    }

    // Generated code doesn't repeat user-defined conversions, so these use the runtime accessor whatever the operator's attributes.
    static IEnumerable<(string Name, string[] Dimensions, string Body, string[] SagaTypes)> ConversionOperatorCases()
    {
        var variant = 0;
        foreach (var attribute in Enum.GetValues<MemberAttribute>())
        {
            foreach (var host in Enum.GetValues<ConversionOperatorHost>())
            {
                var property = new Property(RotatingTypes[variant % RotatingTypes.Length], "Id");
                var conversionOperator = $"{AttributeText(attribute)} public static {(variant++ % 2 == 0 ? "implicit" : "explicit")} operator Wrapper(Msg message) => new() {{ Id = {property.Literal("Derived")} }};";
                var body = $$"""
                             public class Msg : ICommand
                             {
                                 public {{property.Type}} Id { get; set; } = {{property.Literal("Msg")}};
                                 {{(host == ConversionOperatorHost.Message ? conversionOperator : "")}}
                             }

                             public class Wrapper
                             {
                                 public {{property.Type}} Id { get; set; } = {{property.Literal("Base")}};
                                 {{(host == ConversionOperatorHost.CastType ? conversionOperator : "")}}
                             }

                             {{Saga("Msg", "CorrelationId", "((Wrapper)m).Id", attribute is MemberAttribute.ObsoleteError or MemberAttribute.ObsoleteInvalidId)}}

                             public class TheSagaData : ContainSagaData
                             {
                                 public {{property.Type}} CorrelationId { get; set; } = {{property.Literal("Data")}};
                             }

                             {{Probe("new Msg()", property)}}
                             """;
                yield return ($"ConversionOperator_{attribute}_On{host}_{property}", ["ConversionOperator", $"{attribute}OnConversionOperator", $"ConversionOperatorOn{host}", RuntimeMessageAccessor], body, ["TheSaga"]);
            }
        }
    }

    const string RuntimeMessageAccessor = nameof(RuntimeMessageAccessor);

    // A nested cast, or a conversion operator picked by the inner cast's type or the checked context, would bind differently when generated code repeats the outer cast.
    static IEnumerable<(string Name, string[] Dimensions, string Body, string[] SagaTypes)> RuntimeReceiverCases()
    {
        foreach (var receiver in Enum.GetValues<RuntimeReceiver>())
        {
            foreach (var propertyType in RotatingTypes)
            {
                var property = new Property(propertyType, "Id");
                string Operator(string parameterType, string tag, string checkedKeyword = "") => $"public static explicit operator {checkedKeyword}Wrapper({parameterType} message) => new() {{ Id = {property.Literal(tag)} }};";
                // The analyzers only accept a checked context around the receiver, not a checked block around the mapping.
                var (wrapperOperators, messageOperators, mapping) = receiver switch
                {
                    RuntimeReceiver.NestedReferenceCasts => ("", "", "((Derived)(Base)m).Id"),
                    RuntimeReceiver.NestedCastToOverloadedConversion => (Operator("Base", "Base") + Operator("Msg", "Msg"), "", "((Wrapper)(Base)m).Id"),
                    RuntimeReceiver.NestedCastThroughObject => (Operator("Msg", "Msg"), "", "((Wrapper)(object)m).Id"),
                    RuntimeReceiver.CheckedConversion => ("", Operator("Msg", "First") + Operator("Msg", "Second", "checked "), "checked((Wrapper)m).Id"),
                    RuntimeReceiver.UncheckedConversion => ("", Operator("Msg", "First") + Operator("Msg", "Second", "checked "), "((Wrapper)m).Id"),
                    _ => throw new ArgumentOutOfRangeException(nameof(receiver))
                };
                var body = $$"""
                             public class Base : ICommand
                             {
                                 public {{property.Type}} Id { get; set; } = {{property.Literal("Base")}};
                             }

                             public class Msg : Base
                             {
                                 {{messageOperators}}
                             }

                             public class Derived : Msg
                             {
                                 public new {{property.Type}} Id { get; set; } = {{property.Literal("Derived")}};
                             }

                             public class Wrapper
                             {
                                 public {{property.Type}} Id { get; set; } = {{property.Literal("IFace")}};
                                 {{wrapperOperators}}
                             }

                             {{Saga("Msg", "CorrelationId", mapping, false)}}

                             public class TheSagaData : ContainSagaData
                             {
                                 public {{property.Type}} CorrelationId { get; set; } = {{property.Literal("Data")}};
                             }

                             {{Probe("new Msg(), new Derived()", property)}}
                             """;
                yield return ($"RuntimeReceiver_{receiver}_{property}", [receiver.ToString(), property.TypeDimension, RuntimeMessageAccessor], body, ["TheSaga"]);
            }
        }
    }

    const string UnderscoreNames = nameof(UnderscoreNames);

    // Two sagas whose message or saga data type and property names join to the same text with an underscore between them.
    static IEnumerable<(string Name, string[] Dimensions, string Body, string[] SagaTypes)> UnderscoreNameCases()
    {
        foreach (var propertyType in RotatingTypes)
        {
            var type = new Property(propertyType, "Id").Type;
            (string Name, string FirstType, string FirstProperty, string SecondType, string SecondProperty)[] messages =
            [
                ("TypeUnderscore", "Order_Id", "Id", "Order", "Id_Id"),
                ("TrailingUnderscore", "Order_", "Id", "Order", "_Id")
            ];
            (string Name, string FirstType, string FirstProperty, string SecondType, string SecondProperty)[] sagaData =
            [
                .. propertyType == PropertyType.Guid ? [] : new[] { ("PropertyTypeUnderscore", $"Data_{type}", "Prop", "Data", $"{type}_Prop") },
                ("TrailingUnderscore", "Data_", "Prop", "Data", "_Prop")
            ];
            foreach (var message in messages)
            {
                foreach (var data in sagaData)
                {
                    var first = new Property(propertyType, message.FirstProperty);
                    var second = new Property(propertyType, message.SecondProperty);
                    var body = $$"""
                                 public class {{message.FirstType}} : ICommand
                                 {
                                     public {{type}} {{message.FirstProperty}} { get; set; } = {{first.Literal("Msg")}};
                                 }

                                 public class {{message.SecondType}} : ICommand
                                 {
                                     public {{type}} {{message.SecondProperty}} { get; set; } = {{second.Literal("Other")}};
                                 }

                                 {{Saga(message.FirstType, data.FirstProperty, $"m.{message.FirstProperty}", false, sagaData: data.FirstType)}}

                                 public class {{data.FirstType}} : ContainSagaData
                                 {
                                     public {{type}} {{data.FirstProperty}} { get; set; } = {{first.Literal("Data")}};
                                 }

                                 {{Saga(message.SecondType, data.SecondProperty, $"m.{message.SecondProperty}", false, "SecondSaga", data.SecondType)}}

                                 public class {{data.SecondType}} : ContainSagaData
                                 {
                                     public {{type}} {{data.SecondProperty}} { get; set; } = {{second.Literal("Data")}};
                                 }

                                 {{Probe($"new {message.FirstType}(), new {message.SecondType}()", first)}}
                                 """;
                    yield return ($"UnderscoreNames_{message.Name}Message_{data.Name}SagaData_{propertyType}", [UnderscoreNames, $"{message.Name}MessageNames", $"{data.Name}SagaDataNames", propertyType.ToString()], body, ["TheSaga", "SecondSaga"]);
                }
            }
        }
    }

    static bool IsPossible(MessageKind kind, MessageDeclaration declaration, Receiver receiver, MessageAccess access)
    {
        var hasDerived = kind is not (MessageKind.SealedClass or MessageKind.Struct);
        var onInterface = declaration is MessageDeclaration.ImplicitInterface or MessageDeclaration.ExplicitInterface or MessageDeclaration.ExplicitInterfaceOnBaseClass or MessageDeclaration.DefaultInterfaceMember
            or MessageDeclaration.ReimplementedExplicitlyInDerived or MessageDeclaration.ReimplementedWithNewInDerived;

        if (!hasDerived && declaration is MessageDeclaration.ReimplementedExplicitlyInDerived or MessageDeclaration.ReimplementedWithNewInDerived)
        {
            return false;
        }

        // A struct has no base class and can't declare protected members.
        if (kind == MessageKind.Struct && (declaration is not (MessageDeclaration.OnType or MessageDeclaration.ImplicitInterface or MessageDeclaration.ExplicitInterface or MessageDeclaration.DefaultInterfaceMember) || access == MessageAccess.ProtectedGetter))
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

    static (string, string[], string, string[]) MessageCase(MessageKind kind, MessageDeclaration declaration, Receiver receiver, MessageAccess access, MemberAttribute attribute, AttributeTarget target, Property property, Variation variation = default)
    {
        var host = access == MessageAccess.PrivateInterface ? "Host." : "";
        var keyword = kind == MessageKind.Record ? "record" : kind == MessageKind.Struct ? "struct" : "class";
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
        var hasDerived = kind is not (MessageKind.SealedClass or MessageKind.Struct);
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

        var obsoleteSaga = attribute is MemberAttribute.ObsoleteError or MemberAttribute.ObsoleteInvalidId || variation.CastTarget == CastTarget.ObsoleteError;
        var correlationType = variation.Conversion == OuterConversion.WideningNumeric ? "long" : property.Type;
        var saga = Saga(mappedType, "CorrelationId", Convert(variation.Conversion, MappingExpression(receiver, property, host, baseType, messageType), property), obsoleteSaga);
        if (variation.SecondReceiver is { } secondReceiver)
        {
            saga += $$"""


                      {{Saga(mappedType, "CorrelationId", MappingExpression(secondReceiver, property, host, baseType, messageType), obsoleteSaga, "SecondSaga")}}

                      public class SecondSagaData : ContainSagaData
                      {
                          public {{correlationType}} CorrelationId { get; set; } = {{property.Literal("Data")}};
                      }
                      """;
        }

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
                                   public {{(kind == MessageKind.SealedClass ? "sealed " : "")}}{{keyword}} {{(kind == MessageKind.ClosedGeneric ? "Envelope<TPayload>" : "Msg")}} : {{(kind == MessageKind.Struct ? "ICommand" : baseType)}}, IFace
                                   {
                                       {{(kind == MessageKind.Struct ? "public Msg() { }" : "")}}
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

                      {{(hasDerived ? $"{CastTargetDeclaration(variation.CastTarget)} {keyword} Derived : {messageType}{derivedInterfaces} {{ {derivedMembers} }}" : "")}}

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
                         public {{correlationType}} CorrelationId { get; set; } = {{property.Literal("Data")}};
                     }

                     {{(variation.CastTarget == CastTarget.ObsoleteError ? "[System.Obsolete]" : "")}}
                     {{Probe(instances, property)}}
                     """;

        var name = $"Message_{kind}_{declaration}_{receiver}_{access}_{AttributeName(attribute, target)}_{property}";
        string[] dimensions = [kind.ToString(), declaration.ToString(), receiver.ToString(), access.ToString(), attribute.ToString(), $"{attribute}On{target}", property.TypeDimension, property.NameDimension];
        string[] sagaTypes = [sagaHost is null ? "TheSaga" : $"{sagaHost}+TheSaga"];
        if (variation.SecondReceiver is { } other)
        {
            name += $"_And{other}";
            dimensions = [.. dimensions, "MultiSaga"];
            sagaTypes = [.. sagaTypes, "SecondSaga"];
        }

        if (variation.Conversion != OuterConversion.None)
        {
            name += $"_{variation.Conversion}Conversion";
            dimensions = [.. dimensions, $"{variation.Conversion}Conversion"];
        }

        if (variation.CastTarget != CastTarget.Plain)
        {
            name += $"_{variation.CastTarget}CastTarget";
            dimensions = [.. dimensions, $"{variation.CastTarget}CastTarget"];
        }

        return (name, dimensions, body, sagaTypes);
    }

    static string Convert(OuterConversion conversion, string mapping, Property property) => conversion switch
    {
        OuterConversion.None => mapping,
        OuterConversion.Identity => $"({property.Type})({mapping})",
        OuterConversion.Boxing => $"(object)({mapping})",
        OuterConversion.WideningNumeric => $"(long)({mapping})",
        OuterConversion.NullableLift => $"({property.Type}?)({mapping})",
        _ => throw new ArgumentOutOfRangeException(nameof(conversion))
    };

    static string CastTargetDeclaration(CastTarget castTarget) => castTarget switch
    {
        CastTarget.Plain => "public",
        CastTarget.FileLocal => "file",
        CastTarget.Obsolete => $"{AttributeText(MemberAttribute.Obsolete)} public",
        CastTarget.ObsoleteCustomId => $"{AttributeText(MemberAttribute.ObsoleteCustomId)} public",
        CastTarget.ObsoleteError => $"{AttributeText(MemberAttribute.ObsoleteError)} public",
        CastTarget.Experimental => $"{AttributeText(MemberAttribute.Experimental)} public",
        _ => throw new ArgumentOutOfRangeException(nameof(castTarget))
    };

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

    static IEnumerable<(string Name, string[] Dimensions, string Body, string[] SagaTypes)> CorrelationCases()
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

        // An extern names the base type it targets, where the user's suppressions don't apply.
        foreach (var baseTypeAttribute in new[] { MemberAttribute.Experimental, MemberAttribute.ObsoleteCustomId })
        {
            foreach (var (accessors, attribute, target) in new[] { (SagaDataAccessors.GetSet, MemberAttribute.None, AttributeTarget.Property), (SagaDataAccessors.GetInit, MemberAttribute.None, AttributeTarget.Property), (SagaDataAccessors.GetSet, MemberAttribute.ObsoleteError, AttributeTarget.Getter) })
            {
                yield return CorrelationCase(SagaDataDeclaration.OnBaseClass, accessors, attribute, target, new Property(RotatingTypes[variant++ % RotatingTypes.Length], "OrderId"), baseTypeAttribute);
            }
        }
    }

    static (string, string[], string, string[]) CorrelationCase(SagaDataDeclaration declaration, SagaDataAccessors accessors, MemberAttribute attribute, AttributeTarget target, Property property, MemberAttribute baseTypeAttribute = MemberAttribute.None)
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
        var saga = Saga("Msg", property.Name, mapping, attribute is MemberAttribute.ObsoleteError or MemberAttribute.ObsoleteInvalidId);
        var (sagaData, sagaHost) = declaration switch
        {
            SagaDataDeclaration.OnSagaData => ($"public class TheSagaData : ContainSagaData {{ {Declare("", property.Type, property.Literal("Data"))} {(nestSaga ? saga : "")} }}", "TheSagaData"),
            SagaDataDeclaration.OnBaseClass => ($"{AttributeText(baseTypeAttribute)} public class DataBase : ContainSagaData {{ {Declare("", property.Type, property.Literal("Data"))} {(nestSaga ? saga : "")} }} public class TheSagaData : DataBase {{ }}", "DataBase"),
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
        if (baseTypeAttribute != MemberAttribute.None)
        {
            name += $"_{baseTypeAttribute}BaseType";
            dimensions = [.. dimensions, $"{baseTypeAttribute}BaseType"];
        }

        return (name, dimensions, body, [nestSaga ? $"{sagaHost}+TheSaga" : "TheSaga"]);
    }

    static string Saga(string mappedType, string sagaProperty, string mapping, bool obsolete, string name = "TheSaga", string sagaData = null) =>
        $$"""
          [Saga]
          {{(obsolete ? "[System.Obsolete]" : "")}}
          public class {{name}} : Saga<{{sagaData ?? $"{name}Data"}}>, IAmStartedByMessages<{{mappedType}}>
          {
              protected override void ConfigureHowToFindSaga(SagaPropertyMapper<{{sagaData ?? $"{name}Data"}}> mapper) =>
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
            var probe = assembly.GetType($"{differentialCase.Namespace}.Probe", throwOnError: true)!;
            var registration = Describe(() => Register(assembly, configuration, differentialCase));
            foreach (var sagaTypeName in differentialCase.SagaTypes)
            {
                var sagaType = assembly.GetType(sagaTypeName, throwOnError: true)!;
                var registered = registration.Value is SagaMetadataCollection sagas ? Describe(() => sagas.Find(sagaType)) : registration;
                var prefix = differentialCase.SagaTypes.Length > 1 ? $"{sagaType.Name}: " : "";
                var (generatedMessageAccessor, generatedCorrelationAccessor) = Execute(assembly, probe, sagaType, registered, failures, prefix, differentialCase.ReadsMappedTypeImplementationOfDerived);
                outcome = outcome with
                {
                    GeneratedMessageAccessors = outcome.GeneratedMessageAccessors + (generatedMessageAccessor ? 1 : 0),
                    GeneratedCorrelationAccessors = outcome.GeneratedCorrelationAccessors + (generatedCorrelationAccessor ? 1 : 0)
                };
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception.ToString());
        }

        return outcome;
    }

    static (bool GeneratedMessageAccessor, bool GeneratedCorrelationAccessor) Execute(Assembly assembly, Type probe, Type sagaType, (object Value, string Description) registered, List<string> failures, string prefix, bool readsMappedTypeImplementationOfDerived)
    {
        var expected = Describe(() => SagaAccessorCompilation.ExpressionBasedMetadata(sagaType));
        if (registered.Value is not SagaMetadata registeredMetadata || expected.Value is not SagaMetadata expectedMetadata)
        {
            if (registered.Description != expected.Description)
            {
                failures.Add($"{prefix}Creating the saga metadata: registered {registered.Description}, compiled from the mapping {expected.Description}");
            }

            return (false, false);
        }

        var messageType = registeredMetadata.AssociatedMessages.Single().MessageType;
        var registeredMessageAccessor = MessageAccessor(registeredMetadata, messageType);
        var expectedMessageAccessor = MessageAccessor(expectedMetadata, messageType);
        var messages = (object[])probe.GetMethod("Messages")!.Invoke(null, null)!;
        foreach (var message in messages)
        {
            var readAs = readsMappedTypeImplementationOfDerived && message.GetType().Name == "Derived" ? messages.Single(m => m.GetType() == messageType) : message;
            Compare(failures, $"{prefix}Reading a {message.GetType().Name}", () => registeredMessageAccessor.AccessFrom(message), () => expectedMessageAccessor.AccessFrom(readAs));
        }

        if (!registeredMetadata.TryGetCorrelationProperty(out var registeredCorrelation) || !expectedMetadata.TryGetCorrelationProperty(out var expectedCorrelation))
        {
            throw new InvalidOperationException("The saga metadata has no correlation property.");
        }

        var registeredAccessor = registeredCorrelation.Accessor;
        var expectedAccessor = expectedCorrelation.Accessor;

        IContainSagaData NewSagaData() => (IContainSagaData)Activator.CreateInstance(registeredMetadata.SagaEntityType)!;
        Compare(failures, $"{prefix}Reading new saga data", () => registeredAccessor.AccessFrom(NewSagaData()), () => expectedAccessor.AccessFrom(NewSagaData()));
        foreach (var value in (object[])probe.GetMethod("Values")!.Invoke(null, null)!)
        {
            var writtenByRegistered = NewSagaData();
            var writtenByExpected = NewSagaData();
            Compare(failures, $"{prefix}Writing {value}", () => Write(registeredAccessor, writtenByRegistered, value), () => Write(expectedAccessor, writtenByExpected, value));
            Compare(failures, $"{prefix}Reading {value} written by the registered accessor", () => expectedAccessor.AccessFrom(writtenByRegistered), () => expectedAccessor.AccessFrom(writtenByExpected));
            Compare(failures, $"{prefix}Reading {value} with the registered accessor", () => registeredAccessor.AccessFrom(writtenByExpected), () => expectedAccessor.AccessFrom(writtenByExpected));
        }

        return (registeredMessageAccessor.GetType().Assembly == assembly, registeredAccessor.GetType().Assembly == assembly);
    }

    static SagaMetadataCollection Register(Assembly assembly, EndpointConfiguration configuration, DifferentialCase differentialCase)
    {
        assembly.GetType("Test")!.GetMethod("Register")!.Invoke(null, [configuration, differentialCase.Namespace]);
        return configuration.GetSettings().Get<SagaMetadataCollection>();
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

    sealed record DifferentialCase(string Name, string[] Dimensions, string Namespace, string Source, string[] SagaTypes)
    {
        // Known limitation until Particular/NServiceBus#7968 lets such messages register: the generated accessor reads the mapped type's implementation.
        public bool ReadsMappedTypeImplementationOfDerived =>
            Dimensions.Contains(nameof(MessageAccess.PrivateInterface)) && Dimensions.Contains(nameof(Receiver.CastToInterface))
            && Dimensions.Any(dimension => dimension is nameof(MessageDeclaration.ReimplementedExplicitlyInDerived) or nameof(MessageDeclaration.ReimplementedWithNewInDerived))
            && !Dimensions.Contains(nameof(MessageKind.AbstractBase))
            && !(Dimensions.Contains(nameof(MessageKind.ClosedGeneric)) && Dimensions.Any(dimension => dimension is nameof(MemberAttribute.ObsoleteError) or nameof(MemberAttribute.ObsoleteInvalidId)));

        public bool UsesRuntimeMessageAccessor => Dimensions.Contains(RuntimeMessageAccessor);

        public bool HasUnderscoreNames => Dimensions.Contains(UnderscoreNames);
    }

    sealed record Outcome(string RejectedBy, IReadOnlyList<string> Failures, string Details)
    {
        public int GeneratedMessageAccessors { get; init; }
        public int GeneratedCorrelationAccessors { get; init; }
        public bool GeneratedMessageAccessor => GeneratedMessageAccessors > 0;
        public bool GeneratedCorrelationAccessor => GeneratedCorrelationAccessors > 0;

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

    // Only classes implement interface messages, because the runtime accessor's CompileFast returns the boxed struct for a cast to a struct instead of reading the property.
    enum MessageKind { Class, SealedClass, AbstractBase, Interface, ClosedGeneric, NestedType, Record, Struct }

    enum MessageDeclaration { OnType, OnBaseClass, ImplicitInterface, ExplicitInterface, ExplicitInterfaceOnBaseClass, DefaultInterfaceMember, GenericBaseClass, HiddenInDerived, OverriddenInDerived, ReimplementedExplicitlyInDerived, ReimplementedWithNewInDerived }

    enum Receiver { Direct, Parenthesized, NullForgiving, BoxedResult, CastToBaseClass, CastToInterface, CastToInterfaceNullForgiving, AsInterface, CastToMessageClass, CastToDerived }

    enum MessageAccess { Public, Internal, Private, ProtectedGetter, PrivateInterface }

    enum MemberAttribute { None, Obsolete, ObsoleteCustomId, ObsoleteCs0618Id, ObsoleteError, Experimental, ObsoleteInvalidId }

    enum AttributeTarget { Property, Getter, Setter }

    enum PropertyType { String, Guid, Int, NullableInt }

    enum OuterConversion { None, Identity, Boxing, WideningNumeric, NullableLift }

    enum CastTarget { Plain, FileLocal, Obsolete, ObsoleteCustomId, ObsoleteError, Experimental }

    enum ConversionOperatorHost { Message, CastType }

    enum RuntimeReceiver { NestedReferenceCasts, NestedCastToOverloadedConversion, NestedCastThroughObject, CheckedConversion, UncheckedConversion }

    readonly record struct Variation(Receiver? SecondReceiver = null, OuterConversion Conversion = OuterConversion.None, CastTarget CastTarget = CastTarget.Plain);

    enum SagaDataDeclaration { OnSagaData, OnBaseClass, OnGenericBaseClass, OverriddenOnSagaData }

    enum SagaDataAccessors { GetSet, GetInit, GetPrivateSet, GetProtectedSet, PrivateGetSet }
}
