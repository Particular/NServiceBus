namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddSagaInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addSagas = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeAddSagaMethod(node),
                transform: Parser.Parse)
            .Where(static spec => spec.HasValue)
            .Select((spec, _) => spec!.Value)
            .WithTrackingName(TrackingNames.SagaSpec);

        var collected = addSagas.Collect()
            .Select((sagas, _) => new InterceptableSagaSpecs(sagas.ToImmutableEquatableArray()))
            .WithTrackingName(TrackingNames.SagaSpecs);

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }
}