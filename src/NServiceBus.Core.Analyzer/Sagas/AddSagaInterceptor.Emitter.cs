namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Linq;
using System.Text;
using Handlers;
using Microsoft.CodeAnalysis;
using Utility;
using static Handlers.AddHandlerInterceptor;

public sealed partial class AddSagaInterceptor
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(SagaSpecs sagaSpecs) => Emit(sourceProductionContext, sagaSpecs);

        static void Emit(SourceProductionContext context, SagaSpecs sagaSpecs)
        {
            var sagas = sagaSpecs.Sagas;
            if (sagas.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter()
                .ForInterceptor()
                .WithGeneratedCodeAttribute();

            sourceWriter.WriteLine("""
                                   static file class InterceptionsOfAddSagaMethod
                                   {
                                   """);

            sourceWriter.Indentation++;

            sourceWriter.WriteLine("""
                                   extension (NServiceBus.EndpointConfiguration endpointConfiguration)
                                   {
                                   """);
            sourceWriter.Indentation++;

            var groups = sagas.Select(s => (MethodName: MethodName(s.SagaName, s.SagaType), Saga: s))
                .GroupBy(i => i.MethodName)
                .OrderBy(g => g.Key, StringComparer.Ordinal);
            foreach (var group in groups)
            {
                (string MethodName, SagaSpec SagaSpec) first = default;
                foreach (var location in group)
                {
                    if (first == default)
                    {
                        first = location;
                    }

                    var (_, saga) = location;
                    sourceWriter.WriteLine($"{saga.Location.Attribute} // {saga.Location.DisplayLocation}");
                }

                (string methodName, SagaSpec sagaSpec) = first;

                sourceWriter.WriteLine($$"""
                                         public void {{methodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("""
                                       System.ArgumentNullException.ThrowIfNull(endpointConfiguration);
                                       var sagaMetadataCollection = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                          .GetOrCreate<NServiceBus.Sagas.SagaMetadataCollection>();
                                       """);

                // Generate builder API calls directly into the source writer
                GenerateBuilderCode(sourceWriter, sagaSpec);
                sourceWriter.WriteLine("sagaMetadataCollection.Add(metadata);");
                sourceWriter.WriteLine();
                AddHandlerInterceptor.Emitter.EmitHandlerRegistryCode(sourceWriter, sagaSpec.Handler);

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

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

            for(var index = 0; index < allPropertyMappings.Length; index++)
            {
                var mapping = allPropertyMappings[index];
                var accessorClassName = AccessorName(mapping);
                _ = sourceWriter.WithGeneratedCodeAttribute();
                sourceWriter.WriteLine($"file sealed class {accessorClassName} : NServiceBus.MessagePropertyAccessor<{mapping.MessageType}>");
                sourceWriter.WriteLine("{");

                sourceWriter.Indentation++;

                sourceWriter.WriteLine($$"""{{accessorClassName}}() { }""");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"protected override object? AccessFrom({mapping.MessageType} message) => AccessFrom_Property(message);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"get_{mapping.MessagePropertyName}\")]");
                sourceWriter.WriteLine($"static extern {mapping.MessagePropertyType} AccessFrom_Property({mapping.MessageType} message);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"public static readonly NServiceBus.MessagePropertyAccessor Instance = new {accessorClassName}();");
                sourceWriter.Indentation--;

                sourceWriter.WriteLine("}");
                if (index < allPropertyMappings.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfAddSagaMethod.g.cs", sourceWriter.ToSourceText());
        }

        static void GenerateBuilderCode(SourceWriter sourceWriter, SagaSpec details)
        {
            sourceWriter.WriteLine("var associatedMessages = new NServiceBus.Sagas.SagaMessage[]");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;
            foreach (var message in details.Handler.Registrations)
            {
                sourceWriter.WriteLine($"new NServiceBus.Sagas.SagaMessage(typeof({message.MessageType}), {(message.RegistrationType == RegistrationType.StartMessageHandler ? "true" : "false")}, {(message.RegistrationType == RegistrationType.TimeoutHandler ? "true" : "false")}),");
            }
            sourceWriter.Indentation--;
            sourceWriter.WriteLine("};");

            sourceWriter.WriteLine("MessagePropertyAccessor[] propertyAccessors = [");
            sourceWriter.Indentation++;
            foreach (var mapping in details.PropertyMappings)
            {
                var accessorClassName = AccessorName(mapping);
                sourceWriter.WriteLine($"{accessorClassName}.Instance,");
            }
            sourceWriter.Indentation--;
            sourceWriter.WriteLine("];");
            sourceWriter.WriteLine($"var metadata = NServiceBus.Sagas.SagaMetadata.Create<{details.SagaType}, {details.SagaDataType}>(associatedMessages, propertyAccessors);");
        }

        static string AccessorName(PropertyMappingSpec mapping)
        {
            var hash = NonCryptographicHash.GetHash(mapping.MessageType, "_", mapping.MessagePropertyName);
            return $"{mapping.MessageName}{mapping.MessagePropertyName}Accessor_{hash:x16}";
        }

        static string MethodName(string sagaName, string sagaType)
        {
            const string NamePrefix = "AddSaga_";

            var sb = new StringBuilder(NamePrefix, 50)
                .Append(sagaName)
                .Append('_');

            var hash = NonCryptographicHash.GetHash(sagaType);

            sb.Append(hash.ToString("x16"));

            return sb.ToString();
        }
    }
}