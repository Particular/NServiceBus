#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Handlers;
using Utility;

public static partial class Sagas
{
    public static class Emitter
    {
        public static void EmitSagaRegistrationBlock(SourceWriter sourceWriter, SagaSpec sagaSpec, string configurationVariable)
        {
            EmitSagaMetadataCollectionVariables(sourceWriter, configurationVariable);
            EmitSagaMetadataAdd(sourceWriter, sagaSpec);

            sourceWriter.WriteLine();

            Handlers.Emitter.EmitHandlerRegistryVariables(sourceWriter, configurationVariable);
            Handlers.Emitter.EmitHandlerRegistryCode(sourceWriter, sagaSpec.Handler);
        }

        static void EmitSagaMetadataCollectionVariables(SourceWriter sourceWriter, string configurationVariable) =>
            sourceWriter.WriteLine($"""
                                    var sagaMetadataCollection = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings({configurationVariable})
                                       .GetOrCreate<NServiceBus.Sagas.SagaMetadataCollection>();
                                    """);

        static void EmitSagaMetadataAdd(SourceWriter sourceWriter, SagaSpec details)
        {
            sourceWriter.WriteLine("var associatedMessages = new NServiceBus.Sagas.SagaMessage[]");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;
            foreach (var message in details.Handler.Registrations)
            {
                sourceWriter.WriteLine($"new NServiceBus.Sagas.SagaMessage(typeof({message.MessageType}), {(message.RegistrationType == Handlers.RegistrationType.StartMessageHandler ? "true" : "false")}, {(message.RegistrationType == Handlers.RegistrationType.TimeoutHandler ? "true" : "false")}),");
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("};");

            sourceWriter.WriteLine("NServiceBus.Sagas.MessagePropertyAccessor[] propertyAccessors = [");
            sourceWriter.Indentation++;
            foreach (var mapping in details.PropertyMappings)
            {
                var propertyAccessorClassName = MessagePropertyAccessorName(mapping);
                sourceWriter.WriteLine($"{propertyAccessorClassName}.Instance,");
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("];");

            // Finder-only sagas have no correlation property and therefore no generated correlation accessor.
            var correlationPropertyAccessor = details.CorrelationPropertyMapping is { } correlationProperty
                ? $"{CorrelationPropertyAccessorName(details.SagaDataFullyQualifiedName, correlationProperty)}.Instance"
                : "null";

            sourceWriter.WriteLine($"var metadata = NServiceBus.Sagas.SagaMetadata.Create<{details.FullyQualifiedName}, {details.SagaDataFullyQualifiedName}>(associatedMessages, {correlationPropertyAccessor}, propertyAccessors);");
            sourceWriter.WriteLine("sagaMetadataCollection.Add(metadata);");
        }

        public static void EmitAccessors(SourceWriter sourceWriter, ImmutableEquatableArray<SagaSpec> sagas)
        {
            EmitMessagePropertyAccessors(sourceWriter, sagas);
            EmitCorrelationPropertyAccessors(sourceWriter, sagas);
        }

        static void EmitMessagePropertyAccessors(SourceWriter sourceWriter, ImmutableEquatableArray<SagaSpec> sagas)
        {
            // Use Dictionary for O(1) deduplication instead of GroupBy
            var uniqueMappings = new Dictionary<MessagePropertyAccessorIdentity, PropertyMappingSpec>();
            foreach (var saga in sagas)
            {
                foreach (var mapping in saga.PropertyMappings)
                {
                    var key = MessagePropertyAccessorIdentity.Of(mapping);
                    if (!uniqueMappings.ContainsKey(key))
                    {
                        uniqueMappings.Add(key, mapping);
                    }
                }
            }

            if (uniqueMappings.Count == 0)
            {
                return;
            }

            // Convert to list and sort once
            var allPropertyMappings = new List<PropertyMappingSpec>(uniqueMappings.Values);
            allPropertyMappings.Sort(static (a, b) => MessagePropertyAccessorIdentity.Of(a).CompareTo(MessagePropertyAccessorIdentity.Of(b)));

            sourceWriter.WriteLine();

            for (var index = 0; index < allPropertyMappings.Count; index++)
            {
                var mapping = allPropertyMappings[index];
                var accessorClassName = MessagePropertyAccessorName(mapping);
                _ = sourceWriter.WithCompilerGeneratedAttribute()
                    .WithGeneratedCodeAttribute();
                sourceWriter.WriteLine($"file sealed class {accessorClassName} : NServiceBus.Sagas.MessagePropertyAccessor<{mapping.MessageType}>");
                sourceWriter.WriteLine("{");

                sourceWriter.Indentation++;

                sourceWriter.WriteLine($$"""{{accessorClassName}}() { }""");
                sourceWriter.WriteLine();
                var member = MemberName(mapping.MessagePropertyName);
                var read = (mapping.ExternGetter, mapping.GetterReceiverCastType) switch
                {
                    (null, null) => $"message.{member}",
                    (null, { } castType) => $"(({castType})message).{member}",
                    (not null, null) => "AccessFrom_Property(message)",
                    (not null, { } castType) => $"AccessFrom_Property(({castType})message)"
                };
                WriteSuppressingDiagnostics(sourceWriter, $"protected override object? AccessFrom({mapping.MessageType} message) => {read};", mapping.SuppressedDiagnosticIds);
                if (mapping.ExternGetter is { } externGetter)
                {
                    WriteExternAccessor(sourceWriter, "AccessFrom_Property", externGetter, mapping.MessagePropertyType, $"{externGetter.ReceiverType} message");
                }

                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"public static readonly NServiceBus.Sagas.MessagePropertyAccessor Instance = new {accessorClassName}();");
                sourceWriter.Indentation--;

                sourceWriter.WriteLine("}");
                if (index < allPropertyMappings.Count - 1)
                {
                    sourceWriter.WriteLine();
                }
            }
        }

        static void WriteExternAccessor(SourceWriter sourceWriter, string methodName, ExternAccessorSpec accessor, string returnType, string parameters)
        {
            var safetyModifier = accessor.UsesUpdatedMemorySafetyRules ? "safe " : "";
            sourceWriter.WriteLine();
            sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"{accessor.MethodName}\")]");
            sourceWriter.WriteLine($"static {safetyModifier}extern {returnType} {methodName}({parameters});");
        }

        static void WriteSuppressingDiagnostics(SourceWriter sourceWriter, string member, ImmutableEquatableArray<string> diagnosticIds)
        {
            if (diagnosticIds.Count == 0)
            {
                sourceWriter.WriteLine(member);
                return;
            }

            var joinedDiagnosticIds = string.Join(", ", diagnosticIds);
            sourceWriter.WriteLine($"#pragma warning disable {joinedDiagnosticIds}");
            sourceWriter.WriteLine(member);
            sourceWriter.WriteLine($"#pragma warning restore {joinedDiagnosticIds}");
        }

        static string MemberName(string name) => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? $"@{name}" : name;

        public static string MessagePropertyAccessorName(PropertyMappingSpec mapping) =>
            $"{mapping.MessageName}{mapping.MessagePropertyName}Accessor_{MessagePropertyAccessorIdentity.Of(mapping).Hash():x16}";

        // Mappings only share an accessor when they read the same way.
        public readonly record struct MessagePropertyAccessorIdentity(string MessageType, string PropertyName, string? AccessedMember, string? ReceiverCastType, string? ExternReceiverType, string? ExternMethodName)
            : IComparable<MessagePropertyAccessorIdentity>
        {
            public static MessagePropertyAccessorIdentity Of(PropertyMappingSpec mapping) =>
                new(mapping.MessageType, mapping.MessagePropertyName, mapping.AccessedMember, mapping.GetterReceiverCastType, mapping.ExternGetter?.ReceiverType, mapping.ExternGetter?.MethodName);

            // Parts that are null leave the names of plain reads unchanged.
            public ulong Hash() =>
                NonCryptographicHash.GetHash(
                    Escape(MessageType), "_", Escape(PropertyName),
                    AccessedMember is null ? "" : $"_{Escape(AccessedMember)}",
                    ReceiverCastType is null ? "" : $"|cast={Escape(ReceiverCastType)}",
                    ExternReceiverType is null ? "" : $"|extern={Escape(ExternReceiverType)}|{Escape(ExternMethodName)}");

            public int CompareTo(MessagePropertyAccessorIdentity other)
            {
                var comparison = string.CompareOrdinal(MessageType, other.MessageType);
                comparison = comparison != 0 ? comparison : string.CompareOrdinal(PropertyName, other.PropertyName);
                comparison = comparison != 0 ? comparison : string.CompareOrdinal(AccessedMember, other.AccessedMember);
                comparison = comparison != 0 ? comparison : string.CompareOrdinal(ReceiverCastType, other.ReceiverCastType);
                comparison = comparison != 0 ? comparison : string.CompareOrdinal(ExternReceiverType, other.ExternReceiverType);
                return comparison != 0 ? comparison : string.CompareOrdinal(ExternMethodName, other.ExternMethodName);
            }
        }

        // Separators are escaped inside a part so one part can't run into the next; parts without them hash as written.
        static string Escape(string? part) => part?.Replace("\\", "\\\\").Replace("_", "\\_").Replace("|", "\\|") ?? "";

        static void EmitCorrelationPropertyAccessors(SourceWriter sourceWriter, ImmutableEquatableArray<SagaSpec> sagas)
        {
            // Keyed by saga-data type too because the generated cast targets the concrete type.
            var uniqueMappings = new Dictionary<CorrelationPropertyAccessorIdentity, (CorrelationPropertyMappingSpec Mapping, string SagaDataType)>();
            foreach (var saga in sagas)
            {
                if (saga.CorrelationPropertyMapping is not { } mapping)
                {
                    continue;
                }

                var key = CorrelationPropertyAccessorIdentity.Of(saga.SagaDataFullyQualifiedName, mapping);
                if (!uniqueMappings.ContainsKey(key))
                {
                    uniqueMappings.Add(key, (mapping, saga.SagaDataFullyQualifiedName));
                }
            }

            if (uniqueMappings.Count == 0)
            {
                return;
            }

            var allPropertyMappings = new List<(CorrelationPropertyMappingSpec Mapping, string SagaDataType)>(uniqueMappings.Values);
            allPropertyMappings.Sort(static (a, b) => CorrelationPropertyAccessorIdentity.Of(a.SagaDataType, a.Mapping).CompareTo(CorrelationPropertyAccessorIdentity.Of(b.SagaDataType, b.Mapping)));

            sourceWriter.WriteLine();

            for (var index = 0; index < allPropertyMappings.Count; index++)
            {
                var (mapping, sagaDataType) = allPropertyMappings[index];
                var accessorClassName = CorrelationPropertyAccessorName(sagaDataType, mapping);
                _ = sourceWriter.WithCompilerGeneratedAttribute()
                    .WithGeneratedCodeAttribute();
                sourceWriter.WriteLine($"file sealed class {accessorClassName} : NServiceBus.Sagas.CorrelationPropertyAccessor");
                sourceWriter.WriteLine("{");

                sourceWriter.Indentation++;

                sourceWriter.WriteLine($$"""{{accessorClassName}}() { }""");
                sourceWriter.WriteLine();
                var member = MemberName(mapping.PropertyName);
                var read = mapping.ExternGetter is { ReceiverType: var getterReceiverType }
                    ? $"AccessFrom_Property(({getterReceiverType})sagaData)"
                    : $"(({sagaDataType})sagaData).{member}";
                var write = mapping.ExternSetter is { ReceiverType: var setterReceiverType }
                    ? $"WriteTo_Property(({setterReceiverType})sagaData, ({mapping.PropertyType})value)"
                    : $"(({sagaDataType})sagaData).{member} = ({mapping.PropertyType})value";

                WriteSuppressingDiagnostics(sourceWriter, $"public override object? AccessFrom(NServiceBus.IContainSagaData sagaData) => {read};", mapping.SuppressedGetterDiagnosticIds);
                if (mapping.ExternGetter is { } externGetter)
                {
                    WriteExternAccessor(sourceWriter, "AccessFrom_Property", externGetter, mapping.PropertyType, $"{externGetter.ReceiverType} sagaData");
                }

                sourceWriter.WriteLine();
                WriteSuppressingDiagnostics(sourceWriter, $"public override void WriteTo(NServiceBus.IContainSagaData sagaData, object value) => {write};", mapping.SuppressedSetterDiagnosticIds);
                if (mapping.ExternSetter is { } externSetter)
                {
                    WriteExternAccessor(sourceWriter, "WriteTo_Property", externSetter, "void", $"{externSetter.ReceiverType} sagaData, {mapping.PropertyType} value");
                }

                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"public static readonly NServiceBus.Sagas.CorrelationPropertyAccessor Instance = new {accessorClassName}();");
                sourceWriter.Indentation--;

                sourceWriter.WriteLine("}");
                if (index < allPropertyMappings.Count - 1)
                {
                    sourceWriter.WriteLine();
                }
            }
        }

        public static string CorrelationPropertyAccessorName(string sagaDataType, CorrelationPropertyMappingSpec mapping) =>
            $"{mapping.PropertyName}As{mapping.PropertyTypeName}Accessor_{CorrelationPropertyAccessorIdentity.Of(sagaDataType, mapping).Hash():x16}";

        // A saga nested in the saga data type can map a hiding member other sagas can't see, whose getter is only reachable through an extern on that type.
        public readonly record struct CorrelationPropertyAccessorIdentity(string SagaDataType, string PropertyType, string PropertyName, string? ExternGetterReceiverType)
            : IComparable<CorrelationPropertyAccessorIdentity>
        {
            public static CorrelationPropertyAccessorIdentity Of(string sagaDataType, CorrelationPropertyMappingSpec mapping) =>
                new(sagaDataType, mapping.PropertyType, mapping.PropertyName, mapping.ExternGetter?.ReceiverType);

            public ulong Hash() =>
                NonCryptographicHash.GetHash(
                    Escape(SagaDataType), "_", Escape(PropertyType), "_", Escape(PropertyName),
                    ExternGetterReceiverType is null ? "" : $"|extern={Escape(ExternGetterReceiverType)}");

            public int CompareTo(CorrelationPropertyAccessorIdentity other)
            {
                var comparison = string.CompareOrdinal(SagaDataType, other.SagaDataType);
                comparison = comparison != 0 ? comparison : string.CompareOrdinal(PropertyType, other.PropertyType);
                comparison = comparison != 0 ? comparison : string.CompareOrdinal(PropertyName, other.PropertyName);
                return comparison != 0 ? comparison : string.CompareOrdinal(ExternGetterReceiverType, other.ExternGetterReceiverType);
            }
        }
    }
}