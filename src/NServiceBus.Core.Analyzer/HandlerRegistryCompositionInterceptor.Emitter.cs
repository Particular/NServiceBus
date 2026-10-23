#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Utility;

public sealed partial class HandlerRegistryCompositionInterceptor
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(CompositionSpec spec) => Emit(sourceProductionContext, spec);

        static void Emit(SourceProductionContext context, CompositionSpec spec)
        {
            if (spec.Calls.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter()
                .ForInterceptor()
                .WithGeneratedCodeAttribute();

            sourceWriter.WriteLine("static file class InterceptionsOfHandlerRegistryAddAllMethods");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("extension (NServiceBus.HandlerRegistry registry)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            var groups = spec.Calls
                .GroupBy(static call => call.Kind)
                .OrderBy(static group => group.Key)
                .ToArray();

            for (var index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                var methodName = GetEntryMethodName(group.Key);

                foreach (var call in group.OrderBy(static call => call.LocationSpec.DisplayLocation, StringComparer.Ordinal))
                {
                    sourceWriter.WriteLine($"{call.LocationSpec.Attribute} // {call.LocationSpec.DisplayLocation}");
                }

                sourceWriter.WriteLine($"public void {InterceptorMethodNameBuilder.Build("HandlerRegistry_", methodName, $"NServiceBus.HandlerRegistry.{methodName}")}()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(registry);");
                foreach (var entry in spec.Entries)
                {
                    sourceWriter.WriteLine($"{entry.EntryTypeName}.{methodName}(registry.Configuration);");
                }

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                if (index < groups.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfHandlerRegistryAddAllMethods.g.cs", sourceWriter.ToSourceText());
        }

        static string GetEntryMethodName(CompositionKind kind) => kind switch
        {
            CompositionKind.All => "AddAll",
            CompositionKind.Handlers => "AddAllHandlers",
            CompositionKind.Sagas => "AddAllSagas",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}
