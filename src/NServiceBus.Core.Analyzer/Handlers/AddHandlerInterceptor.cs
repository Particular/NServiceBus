#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NServiceBus.Core.Analyzer;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var knownTypes = KnownTypePipelines.BuildHandlerKnownTypesPipeline(context);

        var addHandlers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeAddHandlerMethod(node),
                transform: static (ctx, _) => (invocation: (InvocationExpressionSyntax)ctx.Node, semanticModel: ctx.SemanticModel))
            .Combine(knownTypes)
            .Where(static pair =>
            {
                var (_, knownTypes) = pair;
                return knownTypes is not null;
            })
            .Select(static (pair, cancellationToken) =>
            {
                var ((invocation, semanticModel), knownTypes) = pair;
                return Parser.Parse(invocation!, semanticModel!, knownTypes!, cancellationToken);
            })
            .Where(static spec => spec.HasValue)
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName(TrackingNames.HandlerSpec);

        var collected = addHandlers.Collect()
            .Select((handlers, _) => new InterceptableHandlerSpecs(handlers.ToImmutableEquatableArray()))
            .WithTrackingName(TrackingNames.HandlerSpecs);

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }
}