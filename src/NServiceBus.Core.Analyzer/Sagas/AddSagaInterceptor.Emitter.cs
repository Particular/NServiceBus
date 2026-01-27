#nullable enable
namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Utility;

public sealed partial class AddSagaInterceptor
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(InterceptableSagaSpecs sagaSpecs) => Emit(sourceProductionContext, sagaSpecs);

        static void Emit(SourceProductionContext context, InterceptableSagaSpecs sagaSpecs)
        {
            var interceptableSagaSpecs = sagaSpecs.Sagas;
            if (interceptableSagaSpecs.Count == 0)
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

            var groups = interceptableSagaSpecs.Select(h => (MethodName: AddMethodName(h.SagaSpec.Name, h.SagaSpec.FullyQualifiedName), InterceptableHandler: h))
                .GroupBy(i => i.MethodName)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < groups.Length; index++)
            {
                IGrouping<string, (string MethodName, InterceptableSagaSpec Saga)> group = groups[index];
                (string MethodName, InterceptableSagaSpec InterceptableSagaSpec)? first = null;
                foreach (var location in group)
                {
                    first ??= location;

                    var (_, saga) = location;
                    sourceWriter.WriteLine($"{saga.LocationSpec.Attribute} // {saga.LocationSpec.DisplayLocation}");
                }

                if (!first.HasValue)
                {
                    // when we have no location let's skip
                    continue;
                }

                (string methodName, InterceptableSagaSpec interceptableSagaSpec) = first.Value;
                sourceWriter.WriteLine($$"""
                                         public void {{methodName}}()
                                         {
                                         """);
                sourceWriter.Indentation++;

                sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(endpointConfiguration);");

                Sagas.Emitter.EmitSagaMetadataCollectionVariables(sourceWriter, "endpointConfiguration");
                Sagas.Emitter.EmitSagaMetadataAdd(sourceWriter, interceptableSagaSpec.SagaSpec);

                sourceWriter.WriteLine();

                Handlers.Handlers.Emitter.EmitHandlerRegistryVariables(sourceWriter, "endpointConfiguration");
                Handlers.Handlers.Emitter.EmitHandlerRegistryCode(sourceWriter, interceptableSagaSpec.SagaSpec.Handler);

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

            var sagaSpecs2 = interceptableSagaSpecs.Select(s => s.SagaSpec).ToImmutableEquatableArray();
            Sagas.Emitter.EmitMessagePropertyAccessors(sourceWriter, sagaSpecs2);
            Sagas.Emitter.EmitCorrelationPropertyAccessors(sourceWriter, sagaSpecs2);

            sourceWriter.CloseCurlies();

            context.AddSource("InterceptionsOfAddSagaMethod.g.cs", sourceWriter.ToSourceText());
        }

        static string AddMethodName(string name, string handlerType)
        {
            const string NamePrefix = "AddSaga_";

            var sb = new StringBuilder(NamePrefix.Length + name.Length + 1 + 16)
                .Append(NamePrefix)
                .Append(name)
                .Append('_');

            var hash = NonCryptographicHash.GetHash(handlerType);

            sb.Append(hash.ToString("x16"));

            return sb.ToString();
        }
    }
}