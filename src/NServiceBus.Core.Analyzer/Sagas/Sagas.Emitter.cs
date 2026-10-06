#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

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
            var uniqueMappings = new Dictionary<(string MessageType, string MessagePropertyName, string? AccessedMember), PropertyMappingSpec>();
            foreach (var saga in sagas)
            {
                foreach (var mapping in saga.PropertyMappings)
                {
                    var key = (mapping.MessageType, mapping.MessagePropertyName, mapping.AccessedMember);
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
            allPropertyMappings.Sort(static (a, b) =>
            {
                var messageTypeComparison = string.CompareOrdinal(a.MessageType, b.MessageType);
                if (messageTypeComparison != 0)
                {
                    return messageTypeComparison;
                }

                var propertyNameComparison = string.CompareOrdinal(a.MessagePropertyName, b.MessagePropertyName);
                return propertyNameComparison != 0 ? propertyNameComparison : string.CompareOrdinal(a.AccessedMember, b.AccessedMember);
            });

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
                var getterReceiverType = mapping.ExternGetterReceiverType;
                var directReceiver = mapping.GetterReceiverCastType is { } castType ? $"(({castType})message)" : "message";
                var read = getterReceiverType is null ? $"{directReceiver}.{MemberName(mapping.MessagePropertyName)}" : "AccessFrom_Property(message)";
                WriteSuppressingDiagnostics(sourceWriter, $"protected override object? AccessFrom({mapping.MessageType} message) => {read};", mapping.SuppressedDiagnosticIds);
                if (getterReceiverType is not null)
                {
                    WriteExternAccessor(sourceWriter, "AccessFrom_Property", mapping.ExternGetterMethodName!, mapping.MessagePropertyType, $"{getterReceiverType} message", mapping.UsesUpdatedMemorySafetyRules);
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

        static void WriteExternAccessor(SourceWriter sourceWriter, string methodName, string accessorName, string returnType, string parameters, bool usesUpdatedMemorySafetyRules)
        {
            var safetyModifier = usesUpdatedMemorySafetyRules ? "safe " : "";
            sourceWriter.WriteLine();
            sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"{accessorName}\")]");
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

        static string MessagePropertyAccessorName(PropertyMappingSpec mapping)
        {
            var hash = mapping.AccessedMember is { } accessedMember
                ? NonCryptographicHash.GetHash(mapping.MessageType, "_", mapping.MessagePropertyName, "_", accessedMember)
                : NonCryptographicHash.GetHash(mapping.MessageType, "_", mapping.MessagePropertyName);
            return $"{mapping.MessageName}{mapping.MessagePropertyName}Accessor_{hash:x16}";
        }

        static void EmitCorrelationPropertyAccessors(SourceWriter sourceWriter, ImmutableEquatableArray<SagaSpec> sagas)
        {
            // Keyed by saga-data type too because the generated cast targets the concrete type.
            var uniqueMappings = new Dictionary<(string SagaDataType, string PropertyType, string PropertyName), (CorrelationPropertyMappingSpec Mapping, string SagaDataType)>();
            foreach (var saga in sagas)
            {
                if (saga.CorrelationPropertyMapping is not { } mapping)
                {
                    continue;
                }

                var key = (saga.SagaDataFullyQualifiedName, mapping.PropertyType, mapping.PropertyName);
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
            allPropertyMappings.Sort(static (a, b) =>
            {
                var sagaTypeComparison = string.CompareOrdinal(a.SagaDataType, b.SagaDataType);
                if (sagaTypeComparison != 0)
                {
                    return sagaTypeComparison;
                }

                var typeComparison = string.CompareOrdinal(a.Mapping.PropertyType, b.Mapping.PropertyType);
                return typeComparison != 0 ? typeComparison : string.CompareOrdinal(a.Mapping.PropertyName, b.Mapping.PropertyName);
            });

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
                var getterReceiverType = mapping.ExternGetterReceiverType;
                var setterReceiverType = mapping.ExternSetterReceiverType;
                var read = getterReceiverType is null ? $"(({sagaDataType})sagaData).{member}" : $"AccessFrom_Property(({getterReceiverType})sagaData)";
                var write = setterReceiverType is null ? $"(({sagaDataType})sagaData).{member} = ({mapping.PropertyType})value" : $"WriteTo_Property(({setterReceiverType})sagaData, ({mapping.PropertyType})value)";

                WriteSuppressingDiagnostics(sourceWriter, $"public override object? AccessFrom(NServiceBus.IContainSagaData sagaData) => {read};", mapping.SuppressedGetterDiagnosticIds);
                if (getterReceiverType is not null)
                {
                    WriteExternAccessor(sourceWriter, "AccessFrom_Property", $"get_{mapping.PropertyName}", mapping.PropertyType, $"{getterReceiverType} sagaData", mapping.UsesUpdatedMemorySafetyRules);
                }

                sourceWriter.WriteLine();
                WriteSuppressingDiagnostics(sourceWriter, $"public override void WriteTo(NServiceBus.IContainSagaData sagaData, object value) => {write};", mapping.SuppressedSetterDiagnosticIds);
                if (setterReceiverType is not null)
                {
                    WriteExternAccessor(sourceWriter, "WriteTo_Property", $"set_{mapping.PropertyName}", "void", $"{setterReceiverType} sagaData, {mapping.PropertyType} value", mapping.UsesUpdatedMemorySafetyRules);
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

        static string CorrelationPropertyAccessorName(string sagaDataType, CorrelationPropertyMappingSpec mapping)
        {
            var hash = NonCryptographicHash.GetHash(sagaDataType, "_", mapping.PropertyType, "_", mapping.PropertyName);
            return $"{mapping.PropertyName}As{mapping.PropertyTypeMetadataName}Accessor_{hash:x16}";
        }
    }
}