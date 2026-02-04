#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
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

            var correlationPropertyAccessorClassName = CorrelationPropertyAccessorName(details.CorrelationPropertyMapping);
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
            allPropertyMappings.Sort((a, b) =>
            {
                var messageTypeComparison = string.CompareOrdinal(a.MessageType, b.MessageType);
                return messageTypeComparison != 0 ? messageTypeComparison : string.CompareOrdinal(a.MessagePropertyName, b.MessagePropertyName);
            });

            sourceWriter.WriteLine();

            for (var index = 0; index < allPropertyMappings.Count; index++)
            {
                var mapping = allPropertyMappings[index];
                var accessorClassName = MessagePropertyAccessorName(mapping);
                _ = sourceWriter.WithGeneratedCodeAttribute();
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
            var uniqueMappings = new Dictionary<(string PropertyType, string PropertyName), CorrelationPropertyMappingSpec>();
            foreach (var saga in sagas)
            {
                var mapping = saga.CorrelationPropertyMapping;
                var key = (mapping.PropertyType, mapping.PropertyName);
                if (!uniqueMappings.ContainsKey(key))
                {
                    uniqueMappings.Add(key, mapping);
                }
            }

            if (uniqueMappings.Count == 0)
            {
                return;
            }

            var allPropertyMappings = new List<CorrelationPropertyMappingSpec>(uniqueMappings.Values);
            allPropertyMappings.Sort(static (a, b) =>
            {
                var typeComparison = string.CompareOrdinal(a.PropertyType, b.PropertyType);
                return typeComparison != 0 ? typeComparison : string.CompareOrdinal(a.PropertyName, b.PropertyName);
            });

            sourceWriter.WriteLine();

            for (var index = 0; index < allPropertyMappings.Count; index++)
            {
                var mapping = allPropertyMappings[index];
                var accessorClassName = CorrelationPropertyAccessorName(mapping);
                _ = sourceWriter.WithGeneratedCodeAttribute();
                sourceWriter.WriteLine($"file sealed class {accessorClassName} : NServiceBus.Sagas.CorrelationPropertyAccessor");
                sourceWriter.WriteLine("{");

                sourceWriter.Indentation++;

                sourceWriter.WriteLine($$"""{{accessorClassName}}() { }""");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine("public override object? AccessFrom(NServiceBus.IContainSagaData sagaData) => AccessFrom_Property(sagaData);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"get_{mapping.PropertyName}\")]");
                sourceWriter.WriteLine($"static extern {mapping.PropertyType} AccessFrom_Property(NServiceBus.IContainSagaData sagaData);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"public override void WriteTo(NServiceBus.IContainSagaData sagaData, object value) => WriteTo_Property(sagaData, (({mapping.PropertyType})value));");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_{mapping.PropertyName}\")]");
                sourceWriter.WriteLine($"static extern {mapping.PropertyType} WriteTo_Property(NServiceBus.IContainSagaData sagaData, {mapping.PropertyType} value);");
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

        static string CorrelationPropertyAccessorName(CorrelationPropertyMappingSpec mapping)
        {
            var hash = NonCryptographicHash.GetHash(mapping.PropertyType, "_", mapping.PropertyName);
            return $"{mapping.PropertyName}As{mapping.PropertyTypeMetadataName}Accessor_{hash:x16}";
        }
    }
}