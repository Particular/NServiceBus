#nullable enable

namespace NServiceBus.Core.Analyzer.Messages;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NServiceBus.Core.Analyzer;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddMessageTypeInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addMessageTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeAddMessageTypeMethod(node),
                transform: static (ctx, _) => (invocation: (InvocationExpressionSyntax)ctx.Node, semanticModel: ctx.SemanticModel))
            .Select(static (pair, cancellationToken) =>
            {
                var (invocation, semanticModel) = pair;
                return Parser.Parse(invocation!, semanticModel!, cancellationToken);
            })
            .Where(static spec => spec.HasValue)
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName(TrackingNames.MessageTypeSpec);

        var collected = addMessageTypes.Collect()
            .Select((specs, _) => new InterceptableMessageTypeSpecs(specs.ToImmutableEquatableArray()))
            .WithTrackingName(TrackingNames.MessageTypeSpecs);

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }
}
