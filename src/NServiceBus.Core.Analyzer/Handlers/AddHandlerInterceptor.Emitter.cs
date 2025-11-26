namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Utility;

public sealed partial class AddHandlerInterceptor
{
    class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(HandlersSpec handlersSpec) => GenerateInterceptorCode(sourceProductionContext, handlersSpec);

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

        static void GenerateInterceptorCode(SourceProductionContext context, HandlersSpec handlersSpec)
        {
            handlersSpec.Handlers.Deconstruct(out var handlers);

            if (handlers.Length == 0)
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

            var groups = handlers.Select(h => (MethodName: CreateMethodName(h.Name, h.HandlerType), Handler: h))
                .GroupBy(i => i.MethodName)
                .OrderBy(g => g.Key, StringComparer.Ordinal);
            foreach (var group in groups)
            {
                foreach (var location in group)
                {
                    var (_, handler) = location;
                    sourceWriter.WriteLine(
                        $"{handler.LocationSpec.Attribute} // {handler.LocationSpec.DisplayLocation}");
                }

                var (methodName, handlerSpec) = group.First();
                sourceWriter.WriteLine($$"""
                                         public void {{methodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;
                sourceWriter.WriteLine("""
                                       System.ArgumentNullException.ThrowIfNull(endpointConfiguration);
                                       var registry = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                          .GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                       """);
                foreach (var registration in handlerSpec.Registrations.Items)
                {
                    sourceWriter.WriteLine(
                        $"registry.Add{registration.AddType}HandlerForMessage<{handlerSpec.HandlerType}, {registration.MessageType}>();");
                }

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");
            }

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfAddHandlerMethod.g.cs", sourceWriter.ToSourceText());
        }
    }
}
