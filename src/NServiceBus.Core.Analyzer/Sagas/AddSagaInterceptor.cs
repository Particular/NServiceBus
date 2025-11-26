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
        var addSagas = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeAddSagaMethod(node),
                transform: Parser.Parse)
            .Where(static d => d is not null)
            .Select(static (d, _) => d!)
            .WithTrackingName("SagaSpec");

        var collected = addSagas.Collect()
            .Select((sagas, _) => new SagaSpecs(sagas.ToImmutableEquatableArray()))
            .WithTrackingName("SagaSpecs");

        context.RegisterSourceOutput(collected,
            static (productionContext, intercepts) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(intercepts);
            });
    }

    internal readonly record struct SagaSpecs(ImmutableEquatableArray<SagaSpec> Sagas);

    internal record SagaSpec(
        InterceptLocationSpec Location,
        string MethodName,
        string SagaType,
        string SagaDataType,
        ImmutableEquatableArray<PropertyMappingSpec> PropertyMappings,
        HandlerSpec Handler);

    internal record PropertyMappingSpec(string MessageType, string MessagePropertyName);
}