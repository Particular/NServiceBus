#nullable enable
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
        public void Emit(InterceptableHandlerSpecs handlerSpecs) => Emit(sourceProductionContext, handlerSpecs);

        static void Emit(SourceProductionContext context, InterceptableHandlerSpecs handlerSpecs)
        {
            var interceptableHandlers = handlerSpecs.Handlers;
            if (interceptableHandlers.Count == 0)
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

            var groups = interceptableHandlers.Select(h => (MethodName: AddMethodName(h.HandlerSpec.Name, h.HandlerSpec.HandlerType), InterceptableHandler: h))
                .GroupBy(i => i.MethodName)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < groups.Length; index++)
            {
                IGrouping<string, (string MethodName, InterceptableHandlerSpec Handler)> group = groups[index];
                (string MethodName, InterceptableHandlerSpec InterceptableHandlerSpec)? first = null;
                foreach (var location in group)
                {
                    first ??= location;

                    var (_, handler) = location;
                    sourceWriter.WriteLine($"{handler.LocationSpec.Attribute} // {handler.LocationSpec.DisplayLocation}");
                }

                if (!first.HasValue)
                {
                    // when we have no location let's skip
                    continue;
                }

                (string methodName, InterceptableHandlerSpec interceptableHandlerSpec) = first.Value;
                sourceWriter.WriteLine($$"""
                                         public void {{methodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(endpointConfiguration);");

                Handlers.Emitter.EmitHandlerRegistryVariables(sourceWriter, "endpointConfiguration");
                Handlers.Emitter.EmitHandlerRegistryCode(sourceWriter, interceptableHandlerSpec.HandlerSpec);

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

        static string AddMethodName(string name, string handlerType)
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