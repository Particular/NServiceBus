#nullable enable

namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utility;

[Generator(LanguageNames.CSharp)]
public sealed partial class HandlerRegistryCompositionInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var calls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeCompositionCall(node),
                transform: static (ctx, cancellationToken) => Parser.ParseCall((InvocationExpressionSyntax)ctx.Node, ctx.SemanticModel, cancellationToken))
            .Where(static spec => spec.HasValue)
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName(TrackingNames.CallSpec);

        var collectedCalls = calls.Collect()
            .Select(static (specs, _) => specs.ToImmutableEquatableArray())
            .WithTrackingName(TrackingNames.CallSpecs);

        var referencedEntries = collectedCalls
            .Combine(context.CompilationProvider)
            .Select(static (pair, cancellationToken) => pair.Left.Count == 0
                ? ImmutableEquatableArray<EntrySpec>.Empty
                : Parser.DiscoverReferencedEntries(pair.Right, cancellationToken))
            .WithTrackingName(TrackingNames.ReferencedEntries);

        var localEntry = AddHandlerAndSagasRegistrationGenerator.BuildHasRegistryPipeline(context)
            .Combine(context.CompilationProvider.Select(static (compilation, _) => compilation.Assembly.Name))
            .Select(static (pair, _) => pair.Left ? (EntrySpec?)Parser.CreateLocalEntry(pair.Right) : null)
            .WithTrackingName(TrackingNames.LocalEntry);

        var composition = collectedCalls
            .Combine(referencedEntries)
            .Combine(localEntry)
            .Select(static (pair, _) => Parser.CreateComposition(pair.Left.Left, pair.Left.Right, pair.Right))
            .WithTrackingName(TrackingNames.Composition);

        context.RegisterSourceOutput(composition,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }
}
