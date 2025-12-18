#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;
using Utility;
using static Handlers.AddHandlerInterceptor;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddSagaInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => Options.Create(provider))
            .WithTrackingName("SagaOptions");

        var addSagas = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.NServiceBusRegistrationsAttribute",
                predicate: static (_, _) => true,
                transform: Parser.Parse)
            .SelectMany(static (spec, _) => spec)
            .WithTrackingName("SagaSpec");

        var collected = addSagas.Collect()
            .Select((sagas, _) => new SagaSpecs(sagas.ToImmutableEquatableArray()))
            .WithTrackingName("SagaSpecs");

        var sagaSpecsWithOptions = collected.Combine(options);

        context.RegisterSourceOutput(sagaSpecsWithOptions,
            static (productionContext, interceptsAndOptions) =>
            {
                var (intercepts, sagaOptions) = interceptsAndOptions;
                var emitter = new Emitter(productionContext);
                emitter.Emit(intercepts, sagaOptions);
            });
    }

    internal readonly record struct SagaSpecs(ImmutableEquatableArray<SagaSpec> Sagas);

    internal record SagaSpec(
        InterceptLocationSpec Location,
        string SagaName,
        string SagaType,
        string SagaDataType,
        ImmutableEquatableArray<PropertyMappingSpec> PropertyMappings,
        HandlerRegistrationSpec Handler);

    internal record PropertyMappingSpec(string MessageType, string MessageName, string MessagePropertyName, string MessagePropertyType);
}