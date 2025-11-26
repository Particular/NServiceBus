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
            if (handlerSpecs.Handlers.Count == 0)
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

            var handlers = handlerSpecs.Handlers;
            var groups = handlers.Select(h => (MethodName: CreateMethodName(h.Name, h.HandlerType), Handler: h))
                .GroupBy(i => i.MethodName)
                .OrderBy(g => g.Key, StringComparer.Ordinal);
            foreach (var group in groups)
            {
                (string MethodName, HandlerSpec HandlerSpec) first = default;
                foreach (var location in group)
                {
                    if (first == default)
                    {
                        first = location;
                    }

                    var (_, handler) = location;
                    sourceWriter.WriteLine(
                        $"{handler.LocationSpec.Attribute} // {handler.LocationSpec.DisplayLocation}");
                }

                (string methodName, HandlerSpec handlerSpec) = first;
                sourceWriter.WriteLine($$"""
                                         public void {{methodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(endpointConfiguration);");

                EmitHandlerRegistryCode(sourceWriter, handlerSpec);

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");
            }

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfAddHandlerMethod.g.cs", sourceWriter.ToSourceText());
        }

        public static void EmitHandlerRegistryCode(SourceWriter sourceWriter, HandlerSpec handlerSpec)
        {
            sourceWriter.WriteLine("""
                                   var registry = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                      .GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                   """);
            foreach (var registration in handlerSpec.Registrations)
            {
                var addType = registration.RegistrationType switch
                {
                    RegistrationType.MessageHandler or RegistrationType.StartMessageHandler => "Message",
                    RegistrationType.TimeoutHandler => "Timeout",
                    _ => "Message"
                };

                sourceWriter.WriteLine($"registry.Add{addType}HandlerForMessage<{handlerSpec.HandlerType}, {registration.MessageType}>();");
            }
        }

        static string CreateMethodName(string name, string handlerType)
        {
            const string NamePrefix = "AddHandler_";

            var sb = new StringBuilder(NamePrefix, 50)
                .Append(name)
                .Append('_');

            var hash = NonCryptographicHash.GetHash(handlerType);

            sb.Append(hash.ToString("x16"));

            return sb.ToString();
        }
    }
}