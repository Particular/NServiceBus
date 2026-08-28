#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System.Collections.Generic;
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

            var correlationPropertyAccessorClassName = CorrelationPropertyAccessorName(details.SagaDataFullyQualifiedName, details.CorrelationPropertyMapping);
            var correlationPropertyAccessor = $"{correlationPropertyAccessorClassName}.Instance";

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("];");
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
            var uniqueMappings = new Dictionary<(string MessageType, string MessagePropertyName), PropertyMappingSpec>();
            foreach (var saga in sagas)
            {
                foreach (var mapping in saga.PropertyMappings)
                {
                    var key = (mapping.MessageType, mapping.MessagePropertyName);
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
                return messageTypeComparison != 0 ? messageTypeComparison : string.CompareOrdinal(a.MessagePropertyName, b.MessagePropertyName);
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
                sourceWriter.WriteLine($"protected override object? AccessFrom({mapping.MessageType} message) => AccessFrom_Property(message);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"get_{mapping.MessagePropertyName}\")]");
                sourceWriter.WriteLine($"static extern {mapping.MessagePropertyType} AccessFrom_Property({mapping.MessageType} message);");
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

        static string MessagePropertyAccessorName(PropertyMappingSpec mapping)
        {
            var hash = NonCryptographicHash.GetHash(mapping.MessageType, "_", mapping.MessagePropertyName);
            return $"{mapping.MessageName}{mapping.MessagePropertyName}Accessor_{hash:x16}";
        }

        static void EmitCorrelationPropertyAccessors(SourceWriter sourceWriter, ImmutableEquatableArray<SagaSpec> sagas)
        {
            // Accessors are keyed by the concrete saga-data type plus property identity: two saga-data classes with
            // the same correlation property name and type must not share an accessor, because the UnsafeAccessor
            // receiver is the concrete saga-data type.
            var uniqueMappings = new Dictionary<(string SagaDataType, string PropertyType, string PropertyName), (CorrelationPropertyMappingSpec Mapping, string SagaDataType)>();
            foreach (var saga in sagas)
            {
                var mapping = saga.CorrelationPropertyMapping;
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
                sourceWriter.WriteLine($"public override object? AccessFrom(NServiceBus.IContainSagaData sagaData) => AccessFrom_Property(({sagaDataType})sagaData);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"get_{mapping.PropertyName}\")]");
                sourceWriter.WriteLine($"static extern {mapping.PropertyType} AccessFrom_Property({sagaDataType} sagaData);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"public override void WriteTo(NServiceBus.IContainSagaData sagaData, object value) => WriteTo_Property(({sagaDataType})sagaData, (({mapping.PropertyType})value));");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_{mapping.PropertyName}\")]");
                sourceWriter.WriteLine($"static extern void WriteTo_Property({sagaDataType} sagaData, {mapping.PropertyType} value);");
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