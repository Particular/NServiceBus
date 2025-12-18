namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Utility;

public sealed partial class AddHandlerInterceptor
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(HandlerSpecs handlerSpecs) => Emit(sourceProductionContext, handlerSpecs);

        static void Emit(SourceProductionContext context, HandlerSpecs handlerSpecs)
        {
            var handlers = handlerSpecs.Handlers;
            if (handlers.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter()
                .ForInterceptor()
                .WithGeneratedCodeAttribute();

            sourceWriter.WriteLine("""
                                   static file class InterceptionsOfAddHandlerMethod
                                   {
                                   """);

            sourceWriter.Indentation++;

            sourceWriter.WriteLine("""
                                   extension (NServiceBus.EndpointConfiguration endpointConfiguration)
                                   {
                                   """);
            sourceWriter.Indentation++;

            var groups = handlers
                .GroupBy(h => h.RegistrationInfo.HandlerType)
                .OrderBy(g => g.Key.InterceptorMethodName, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                foreach (var location in group)
                {
                    sourceWriter.WriteLine($"{location.LocationSpec.Attribute} // {location.LocationSpec.DisplayLocation}");
                }

                HandlerSpec first = group.First();
                sourceWriter.WriteLine($$"""
                                         public void {{first.RegistrationInfo.HandlerType.InterceptorMethodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(endpointConfiguration);");

                EmitHandlerRegistryCode(sourceWriter, first.RegistrationInfo);

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                if (index < groups.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfAddHandlerMethod.g.cs", sourceWriter.ToSourceText());
        }

        public static void EmitHandlerRegistryCode(SourceWriter sourceWriter, HandlerRegistrationSpec handlerSpec)
        {
            sourceWriter.WriteLine("""
                                   var settings = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration);
                                   var messageHandlerRegistry = settings.GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                   var messageMetadataRegistry = settings.GetOrCreate<NServiceBus.Unicast.Messages.MessageMetadataRegistry>();
                                   """);
            foreach (var registration in handlerSpec.Registrations)
            {
                var addType = registration.RegistrationType switch
                {
                    RegistrationType.MessageHandler or RegistrationType.StartMessageHandler => "Message",
                    RegistrationType.TimeoutHandler => "Timeout",
                    _ => "Message"
                };

                sourceWriter.WriteLine($"messageHandlerRegistry.Add{addType}HandlerForMessage<{handlerSpec.HandlerType.FullyQualifiedName}, {registration.MessageType}>();");
                var hierarchyLiteral = $"[{string.Join(", ", registration.MessageHierarchy.Select(type => $"typeof({type})"))}]";
                sourceWriter.WriteLine($"messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof({registration.MessageType}), {hierarchyLiteral});");
            }
        }
    }
}