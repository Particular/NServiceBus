#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;
using Utility;
using static Handlers.AddHandlerInterceptor;
using static NServiceBus.Core.Analyzer.ManualRegistrations;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddSagaInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => Options.Create(provider))
            .WithTrackingName("SagaOptions");

        var registrationTargets = CreateTargets(context);

        var addSagas = registrationTargets
            .Combine(context.CompilationProvider)
            .SelectMany(static (targetAndCompilation, cancellationToken) =>
                GetSpecs(targetAndCompilation.Left, targetAndCompilation.Right,
                    Parser.SyntaxLooksLikeAddSagaMethod,
                    static (semanticModel, invocation, ct) => Parser.Parse(semanticModel, invocation, ct),
                    cancellationToken))
            .WithTrackingName("SagaSpec");

        var collected = addSagas
            .Collect()
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
        HandlerSpec Handler);

    internal record PropertyMappingSpec(string MessageType, string MessageName, string MessagePropertyName, string MessagePropertyType);
}
