namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Linq;
using Handlers;
using Utility;

public static partial class Sagas
{
    public static class Emitter
    {
        public static void EmitSagaMetadataCollectionVariables(SourceWriter sourceWriter, string configurationVariable) =>
            sourceWriter.WriteLine($"""
                                    var sagaMetadataCollection = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings({configurationVariable})
                                       .GetOrCreate<NServiceBus.Sagas.SagaMetadataCollection>();
                                    """);

        public static void EmitSagaMetadataCreate(SourceWriter sourceWriter, SagaSpec details)
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
                var accessorClassName = MessagePropertyAccessorName(mapping);
                sourceWriter.WriteLine($"{accessorClassName}.Instance,");
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("];");
            sourceWriter.WriteLine($"var metadata = NServiceBus.Sagas.SagaMetadata.Create<{details.FullyQualifiedName}, {details.SagaDataFullyQualifiedName}>(associatedMessages, null, propertyAccessors);");
        }

        public static void EmitMessagePropertyAccessors(SourceWriter sourceWriter, ImmutableEquatableArray<SagaSpec> sagas)
        {
            var allPropertyMappings = sagas
                .SelectMany(i => i.PropertyMappings)
                .GroupBy(m => (m.MessageType, m.MessagePropertyName))
                .Select(g => g.First())
                .OrderBy(m => m.MessageType, StringComparer.Ordinal)
                .ThenBy(m => m.MessagePropertyName, StringComparer.Ordinal)
                .ToArray();

            if (allPropertyMappings.Length > 0)
            {
                sourceWriter.WriteLine();
            }

            for (var index = 0; index < allPropertyMappings.Length; index++)
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
                if (index < allPropertyMappings.Length - 1)
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
    }
}