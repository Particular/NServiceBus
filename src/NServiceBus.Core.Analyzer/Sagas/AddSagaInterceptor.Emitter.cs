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

            var allPropertyMappings = sagas
                .SelectMany(i => i.PropertyMappings)
                .GroupBy(m => (m.MessageType, m.MessagePropertyName))
                .Select(g => g.First())
                .OrderBy(m => m.MessageType, StringComparer.Ordinal)
                .ThenBy(m => m.MessagePropertyName, StringComparer.Ordinal);

            foreach (var mapping in allPropertyMappings)
            {
                var accessorClassName = $"PropertyAccessor_{CreateAccessorName(mapping.MessageType, mapping.MessagePropertyName)}";
                sourceWriter.WriteLine($"file sealed class {accessorClassName} : NServiceBus.MessagePropertyAccessor<{mapping.MessageType}>");
                sourceWriter.WriteLine("{");

                sourceWriter.Indentation++;

                sourceWriter.WriteLine($"protected override object? AccessFrom({mapping.MessageType} message) => AccessFrom_Property(message);");
                sourceWriter.WriteLine();
                sourceWriter.WriteLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"get_{mapping.MessagePropertyName}\")]");
                sourceWriter.WriteLine($"static extern object? AccessFrom_Property({mapping.MessageType} message);");

                sourceWriter.Indentation--;

                sourceWriter.WriteLine("}");
                sourceWriter.WriteLine();
            }

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

            var groups = sagas.GroupBy(i => i.MethodName)
                .OrderBy(g => g.Key, StringComparer.Ordinal);
            foreach (IGrouping<string, SagaSpec> group in groups)
            {
                foreach (SagaSpec location in group)
                {
                    sourceWriter.WriteLine($"{location.Location.Attribute} // {location.Location.DisplayLocation}");
                }

                SagaSpec first = group.First();

                sourceWriter.WriteLine($$"""
                                         public void {{first.MethodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("""
                                       System.ArgumentNullException.ThrowIfNull(endpointConfiguration);
                                       var sagaMetadataCollection = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                          .GetOrCreate<NServiceBus.Sagas.SagaMetadataCollection>();
                                       """);

                // Generate builder API calls directly into the source writer
                GenerateBuilderCode(sourceWriter, first);
                sourceWriter.WriteLine("sagaMetadataCollection.Add(metadata);");
                AddHandlerInterceptor.Emitter.EmitHandlerRegistryCode(sourceWriter, first.Handler);

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");
            }

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfAddSagaMethod.g.cs", sourceWriter.ToSourceText());
        }

        static void GenerateBuilderCode(SourceWriter sourceWriter, SagaSpec details)
        {
            sourceWriter.WriteLine("var associatedMessages = new NServiceBus.Sagas.SagaMessage[]");
            sourceWriter.WriteLine("{");
            foreach (var message in details.Handler.Registrations)
            {
                sourceWriter.WriteLine($"    new NServiceBus.Sagas.SagaMessage(typeof({message.MessageType}), {(message.RegistrationType == RegistrationType.StartMessageHandler ? "true" : "false")}, {(message.RegistrationType == RegistrationType.TimeoutHandler ? "true" : "false")}),");
            }
            sourceWriter.WriteLine("};");

            // Generate property accessors for property mappings
            if (details.PropertyMappings.Count > 0)
            {
                sourceWriter.WriteLine("var propertyAccessors = new NServiceBus.MessagePropertyAccessor[]");
                sourceWriter.WriteLine("{");
                foreach (var mapping in details.PropertyMappings)
                {
                    var accessorClassName = $"PropertyAccessor_{CreateAccessorName(mapping.MessageType, mapping.MessagePropertyName)}";
                    sourceWriter.WriteLine($"    new {accessorClassName}(),");
                }
                sourceWriter.WriteLine("};");
                sourceWriter.WriteLine($"var metadata = NServiceBus.Sagas.SagaMetadata.Create<{details.SagaType}, {details.SagaDataType}>(associatedMessages, propertyAccessors);");
            }
            else
            {
                sourceWriter.WriteLine($"var metadata = NServiceBus.Sagas.SagaMetadata.Create<{details.SagaType}, {details.SagaDataType}>(associatedMessages);");
            }
        }

        static string CreateAccessorName(string messageType, string propertyName)
        {
            var hash = NonCryptographicHash.GetHash(messageType, "_", propertyName);
            return hash.ToString("x16");
        }
    }
}