#nullable enable

namespace NServiceBus.Core.Analyzer.Messages;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NServiceBus.Core.Analyzer;
using Utility;

public sealed partial class AddMessageTypeInterceptor
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(InterceptableMessageTypeSpecs messageTypeSpecs) => Emit(sourceProductionContext, messageTypeSpecs);

        static void Emit(SourceProductionContext context, InterceptableMessageTypeSpecs messageTypeSpecs)
        {
            var interceptableMessageTypeSpecs = messageTypeSpecs.MessageTypes;
            if (interceptableMessageTypeSpecs.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter()
                .ForInterceptor()
                .WithGeneratedCodeAttribute();

            sourceWriter.WriteLine("""
                                   static file class InterceptionsOfAddMessageTypeMethod
                                   {
                                   """);

            sourceWriter.Indentation++;

            sourceWriter.WriteLine("""
                                   extension (NServiceBus.EndpointConfiguration endpointConfiguration)
                                   {
                                   """);
            sourceWriter.Indentation++;

            var groups = interceptableMessageTypeSpecs.Select(m => (MethodName: AddMethodName(m.MessageTypeSpec.Name, m.MessageTypeSpec.FullyQualifiedName), InterceptableMessageType: m))
                .GroupBy(i => i.MethodName)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < groups.Length; index++)
            {
                IGrouping<string, (string MethodName, InterceptableMessageTypeSpec MessageType)> group = groups[index];
                (string MethodName, InterceptableMessageTypeSpec InterceptableMessageType)? first = null;
                foreach (var location in group)
                {
                    first ??= location;

                    var (_, messageType) = location;
                    sourceWriter.WriteLine($"{messageType.LocationSpec.Attribute} // {messageType.LocationSpec.DisplayLocation}");
                }

                if (!first.HasValue)
                {
                    continue;
                }

                (string methodName, InterceptableMessageTypeSpec interceptableMessageType) = first.Value;
                sourceWriter.WriteLine($$"""
                                         public void {{methodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(endpointConfiguration);");

                EmitMessageTypeRegistration(sourceWriter, interceptableMessageType.MessageTypeSpec);

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                if (index < groups.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfAddMessageTypeMethod.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitMessageTypeRegistration(SourceWriter sourceWriter, MessageTypeSpec messageTypeSpec)
        {
            sourceWriter.WriteLine("""
                                   var settings = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration);
                                   var messageMetadataRegistry = settings.GetOrCreate<NServiceBus.Unicast.Messages.MessageMetadataRegistry>();
                                   """);

            if (messageTypeSpec.HierarchyTypeNames.Count == 0)
            {
                sourceWriter.WriteLine($"messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof({messageTypeSpec.FullyQualifiedName}), []);");
                return;
            }

            sourceWriter.WriteLine($"messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof({messageTypeSpec.FullyQualifiedName}),");
            sourceWriter.WriteLine("[");
            sourceWriter.Indentation++;
            foreach (var hierarchyTypeName in messageTypeSpec.HierarchyTypeNames)
            {
                sourceWriter.WriteLine($"typeof({hierarchyTypeName}),");
            }
            sourceWriter.Indentation--;
            sourceWriter.WriteLine("]);");
        }

        static string AddMethodName(string name, string messageType)
        {
            const string NamePrefix = "AddMessageType_";
            return InterceptorMethodNameBuilder.Build(NamePrefix, name, messageType);
        }
    }
}
