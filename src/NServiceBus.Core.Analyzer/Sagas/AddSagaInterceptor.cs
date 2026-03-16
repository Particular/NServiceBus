namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NServiceBus.Core.Analyzer;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddSagaInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var knownTypes = KnownTypePipelines.BuildHandlerKnownTypesPipeline(context);

        var addSagas = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeAddSagaMethod(node),
                transform: static (ctx, _) => ctx.Node is InvocationExpressionSyntax invocationExpression
                    ? (invocation: invocationExpression, semanticModel: ctx.SemanticModel)
                    : default)
            .Where(static tuple => tuple.invocation is not null)
            .Combine(knownTypes)
            .Select(static (pair, cancellationToken) =>
            {
                var ((invocation, semanticModel), knownTypes) = pair;
                return Parser.Parse(invocation!, semanticModel!, knownTypes, cancellationToken);
            })
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